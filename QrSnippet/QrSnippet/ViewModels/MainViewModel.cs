using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrSnippet.Models;
using QrSnippet.Services;

namespace QrSnippet.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ClipboardService _clipboardService;

        [ObservableProperty]
        private string _statusMessage = "No QR detected";

        public MainViewModel(ClipboardService clipboardService)
        {
            _clipboardService = clipboardService;
        }

        [RelayCommand]
        private void PasteImage()
        {
            ClipboardQrResult result = _clipboardService.ReadQr();

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
    }
}
