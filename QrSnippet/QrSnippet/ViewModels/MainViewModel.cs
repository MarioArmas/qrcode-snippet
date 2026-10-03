using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrSnippet.Models;
using QrSnippet.Services;

namespace QrSnippet.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ClipboardService _clipboardService;
        private readonly ImageFileService _imageFileService;
        private readonly QrDecoderService _decoder;
        private readonly ScreenCaptureService _screenCaptureService;

        [ObservableProperty]
        private string _statusMessage = "No QR detected";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyTextCommand))]
        private string? _decodedText;

        public MainViewModel(
            ClipboardService clipboardService,
            ImageFileService imageFileService,
            ScreenCaptureService screenCaptureService,
            QrDecoderService decoder)
        {
            _clipboardService = clipboardService;
            _imageFileService = imageFileService;
            _screenCaptureService = screenCaptureService;
            _decoder = decoder;
        }

        [RelayCommand]
        private void PasteImage()
        {
            ClipboardQrResult result = _clipboardService.ReadQr();

            DecodedText = result.Text;
            StatusMessage = result.Status switch
            {
                ClipboardQrStatus.Decoded => result.Text!,
                ClipboardQrStatus.ContainsText => "The clipboard contains text, not an image",
                ClipboardQrStatus.NoQrFound => "No QR detected in the pasted image",
                ClipboardQrStatus.Empty => "The clipboard is empty",
                ClipboardQrStatus.ClipboardBusy => "The clipboard is in use by another app, try again",
                _ => "No QR detected"
            };
        }

        [RelayCommand]
        private void OpenImage()
        {
            string? path = _imageFileService.PickImagePath();
            if (path is null)
            {
                return;
            }

            var image = _imageFileService.LoadImage(path);
            if (image is null)
            {
                DecodedText = null;
                StatusMessage = "The selected file is not a valid image";
                return;
            }

            DecodedText = _decoder.Decode(image);
            StatusMessage = DecodedText ?? "No QR detected in the selected image";
        }

        // AsyncRelayCommand disables the button while a capture is in progress.
        [RelayCommand]
        private async Task CaptureAsync()
        {
            var image = await _screenCaptureService.CaptureRegionAsync();
            if (image is null)
            {
                return;
            }

            DecodedText = _decoder.Decode(image);
            StatusMessage = DecodedText ?? "No QR detected in the selected area";
        }

        [RelayCommand(CanExecute = nameof(CanCopyText))]
        private void CopyText()
        {
            if (!_clipboardService.SetText(DecodedText!))
            {
                StatusMessage = "The clipboard is in use by another app, try again";
            }
        }

        private bool CanCopyText() => !string.IsNullOrEmpty(DecodedText);
    }
}
