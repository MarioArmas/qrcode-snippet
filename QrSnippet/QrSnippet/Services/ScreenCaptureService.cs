using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using QrSnippet.Views;

namespace QrSnippet.Services
{
    /// <summary>
    /// Freezes the screen and lets the user drag a rectangle over it, like the Windows Snipping Tool.
    /// </summary>
    public class ScreenCaptureService
    {
        // Gives the hidden main window time to fade out before the screen is captured.
        private const int HideDelayMs = 250;

        /// <summary>
        /// Returns the selected area as an image, or null if the user cancels.
        /// </summary>
        public async Task<BitmapSource?> CaptureRegionAsync()
        {
            Window? mainWindow = Application.Current.MainWindow;
            mainWindow?.Hide();

            try
            {
                await Task.Delay(HideDelayMs);

                Int32Rect virtualScreen = NativeMethods.GetVirtualScreenBounds();
                BitmapSource screenshot = CaptureScreen(virtualScreen);

                Int32Rect? selection = await SelectRegionAsync(screenshot, virtualScreen);
                if (selection is null)
                {
                    return null;
                }

                var region = new CroppedBitmap(screenshot, selection.Value);
                region.Freeze();
                return region;
            }
            finally
            {
                if (mainWindow is not null)
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                }
            }
        }

        private static BitmapSource CaptureScreen(Int32Rect bounds)
        {
            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr bitmap = NativeMethods.CreateCompatibleBitmap(screenDc, bounds.Width, bounds.Height);
            IntPtr previous = NativeMethods.SelectObject(memoryDc, bitmap);

            try
            {
                // CAPTUREBLT includes layered (semi-transparent) windows in the capture.
                NativeMethods.BitBlt(memoryDc, 0, 0, bounds.Width, bounds.Height,
                    screenDc, bounds.X, bounds.Y, NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);
                NativeMethods.SelectObject(memoryDc, previous);

                BitmapSource image = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                image.Freeze();
                return image;
            }
            finally
            {
                NativeMethods.DeleteObject(bitmap);
                NativeMethods.DeleteDC(memoryDc);
                NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        // Shows one overlay per monitor so each one renders at its own DPI.
        private static Task<Int32Rect?> SelectRegionAsync(BitmapSource screenshot, Int32Rect virtualScreen)
        {
            var completion = new TaskCompletionSource<Int32Rect?>();
            var overlays = new List<ScreenCaptureOverlay>();

            void Finish(Int32Rect? selection)
            {
                if (completion.TrySetResult(selection))
                {
                    overlays.ForEach(overlay => overlay.Close());
                }
            }

            foreach (Int32Rect monitor in NativeMethods.GetMonitorBounds())
            {
                // The same area expressed relative to the screenshot's top-left corner.
                var imageBounds = new Int32Rect(
                    monitor.X - virtualScreen.X, monitor.Y - virtualScreen.Y, monitor.Width, monitor.Height);

                var overlay = new ScreenCaptureOverlay(screenshot, imageBounds, monitor);
                overlay.SelectionCompleted += Finish;
                overlay.Closed += (_, _) => Finish(null);
                overlays.Add(overlay);
            }

            overlays.ForEach(overlay => overlay.Show());
            return completion.Task;
        }
    }
}
