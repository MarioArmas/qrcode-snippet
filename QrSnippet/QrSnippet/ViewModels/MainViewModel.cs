using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrSnippet.Models;
using QrSnippet.Properties;
using QrSnippet.Services;

namespace QrSnippet.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const int FeedbackDurationMs = 2500;

        private readonly ClipboardService _clipboardService;
        private readonly ImageFileService _imageFileService;
        private readonly QrDecoderService _decoder;
        private readonly ScreenCaptureService _screenCaptureService;
        private readonly LinkService _linkService;
        private readonly HistoryService _historyService;
        private CancellationTokenSource? _feedbackCts;

        // Incremented on every new scan or problem so a slower, older decode cannot overwrite it.
        private int _scanId;

        [ObservableProperty]
        private ScanState _state = ScanState.Idle;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _statusMessage = Strings.NothingScanned;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyTextCommand))]
        private string? _decodedText;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenLinkCommand))]
        private Uri? _detectedUrl;

        [ObservableProperty]
        private BitmapSource? _previewImage;

        /// <summary>
        /// Short-lived confirmation such as "Copied to clipboard"; cleared automatically.
        /// </summary>
        [ObservableProperty]
        private string? _feedback;

        public MainViewModel(
            ClipboardService clipboardService,
            ImageFileService imageFileService,
            ScreenCaptureService screenCaptureService,
            LinkService linkService,
            QrDecoderService decoder,
            HistoryService historyService)
        {
            _clipboardService = clipboardService;
            _imageFileService = imageFileService;
            _screenCaptureService = screenCaptureService;
            _linkService = linkService;
            _decoder = decoder;
            _historyService = historyService;

            History = new ObservableCollection<HistoryEntry>(_historyService.Load());
            History.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasHistory));
                ClearHistoryCommand.NotifyCanExecuteChanged();
            };
        }

        /// <summary>
        /// Recent successful scans, newest first.
        /// </summary>
        public ObservableCollection<HistoryEntry> History { get; }

        public bool HasHistory => History.Count > 0;

        // Concurrent executions allowed: a new paste or file replaces a scan still in progress.
        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task PasteImageAsync()
        {
            ClipboardImageResult result = _clipboardService.ReadImage();

            switch (result.Status)
            {
                case ClipboardImageStatus.HasImage:
                    await ScanAsync(result.Image!, Strings.NoQrInPastedImage);
                    break;
                case ClipboardImageStatus.ContainsText:
                    ShowProblem(ScanState.Warning, Strings.ClipboardHasText);
                    break;
                case ClipboardImageStatus.Empty:
                    ShowProblem(ScanState.Warning, Strings.ClipboardEmpty);
                    break;
                case ClipboardImageStatus.ClipboardBusy:
                    ShowProblem(ScanState.Error, Strings.ClipboardBusy);
                    break;
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task OpenImageAsync()
        {
            string? path = _imageFileService.PickImagePath();
            if (path is not null)
            {
                await LoadFileAsync(path);
            }
        }

        /// <summary>
        /// Scans an image file; used by the Open dialog and by drag and drop.
        /// </summary>
        public async Task LoadFileAsync(string path)
        {
            var image = _imageFileService.LoadImage(path);
            if (image is null)
            {
                ShowProblem(ScanState.Error, Strings.InvalidImage);
                return;
            }

            await ScanAsync(image, Strings.NoQrInSelectedImage);
        }

        // AsyncRelayCommand disables the button while a capture is in progress.
        [RelayCommand]
        private async Task CaptureAsync()
        {
            var image = await _screenCaptureService.CaptureRegionAsync();
            if (image is not null)
            {
                await ScanAsync(image, Strings.NoQrInSelectedArea);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopyText))]
        private void CopyText()
        {
            ShowFeedback(_clipboardService.SetText(DecodedText!)
                ? Strings.CopiedToClipboard
                : Strings.ClipboardBusy);
        }

        private bool CanCopyText() => !string.IsNullOrEmpty(DecodedText);

        [RelayCommand(CanExecute = nameof(CanOpenLink))]
        private void OpenLink()
        {
            ShowFeedback(_linkService.Open(DetectedUrl!)
                ? Strings.OpenedInBrowser
                : Strings.OpenLinkFailed);
        }

        private bool CanOpenLink() => DetectedUrl is not null;

        [RelayCommand]
        private void CopyHistoryEntry(HistoryEntry entry)
        {
            ShowFeedback(_clipboardService.SetText(entry.Text)
                ? Strings.CopiedToClipboard
                : Strings.ClipboardBusy);
        }

        [RelayCommand]
        private void DeleteHistoryEntry(HistoryEntry entry)
        {
            if (History.Remove(entry))
            {
                SaveHistory();
            }
        }

        [RelayCommand(CanExecute = nameof(HasHistory))]
        private void ClearHistory()
        {
            History.Clear();
            SaveHistory();
        }

        private void AddToHistory(string text)
        {
            HistoryService.AddEntry(History, new HistoryEntry(text, DateTimeOffset.Now));
            SaveHistory();
        }

        private void SaveHistory()
        {
            if (!_historyService.Save(History))
            {
                ShowFeedback(Strings.HistorySaveFailed);
            }
        }

        // Shows the image right away and decodes it off the UI thread.
        // notFoundMessage is a full sentence (not just the source) so each language can phrase it naturally.
        private async Task ScanAsync(BitmapSource image, string notFoundMessage)
        {
            int scanId = ++_scanId;

            PreviewImage = image;
            DecodedText = null;
            DetectedUrl = null;
            Feedback = null;
            State = ScanState.Idle;
            StatusMessage = Strings.ScanningStatus;
            IsScanning = true;

            string? text = await _decoder.DecodeAsync(image);

            if (scanId != _scanId)
            {
                return; // Superseded by a newer scan or problem.
            }

            IsScanning = false;
            ShowScan(image, text, notFoundMessage);
        }

        private void ShowScan(BitmapSource image, string? text, string notFoundMessage)
        {
            PreviewImage = image;
            DecodedText = text;
            DetectedUrl = text is not null && LinkService.TryGetWebUrl(text, out Uri? url) ? url : null;
            Feedback = null;

            if (text is null)
            {
                State = ScanState.Warning;
                StatusMessage = notFoundMessage;
                return;
            }

            State = ScanState.Success;
            StatusMessage = DetectedUrl is null ? Strings.QrDetected : Strings.LinkDetected;
            AddToHistory(text);

            if (DetectedUrl is not null)
            {
                OpenLink();
            }
        }

        private void ShowProblem(ScanState state, string message)
        {
            _scanId++;
            IsScanning = false;
            PreviewImage = null;
            DecodedText = null;
            DetectedUrl = null;
            Feedback = null;
            State = state;
            StatusMessage = message;
        }

        // Fire-and-forget: a newer message cancels the pending clear of the previous one.
        private async void ShowFeedback(string message)
        {
            _feedbackCts?.Cancel();
            var cts = _feedbackCts = new CancellationTokenSource();
            Feedback = message;

            try
            {
                await Task.Delay(FeedbackDurationMs, cts.Token);
                Feedback = null;
            }
            catch (TaskCanceledException)
            {
                // Replaced by a newer message.
            }
        }
    }
}
