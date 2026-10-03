using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QrSnippet.Services;

namespace QrSnippet.Views
{
    /// <summary>
    /// Full-screen overlay for one monitor where the user drags a selection rectangle.
    /// </summary>
    public partial class ScreenCaptureOverlay : Window
    {
        // Smaller drags are treated as accidental clicks.
        private const double MinSelectionSize = 4;

        private readonly Int32Rect _imageBounds;
        private readonly Int32Rect _screenBounds;
        private readonly RectangleGeometry _fullArea = new();
        private readonly RectangleGeometry _selectionArea = new();
        private Point? _dragStart;

        /// <summary>
        /// Raised with the selected area in screenshot pixels, or null if the user cancels.
        /// </summary>
        public event Action<Int32Rect?>? SelectionCompleted;

        /// <param name="screenshot">Capture of the whole virtual screen.</param>
        /// <param name="imageBounds">This monitor's area inside the screenshot.</param>
        /// <param name="screenBounds">This monitor's area in screen coordinates.</param>
        public ScreenCaptureOverlay(BitmapSource screenshot, Int32Rect imageBounds, Int32Rect screenBounds)
        {
            InitializeComponent();

            _imageBounds = imageBounds;
            _screenBounds = screenBounds;

            var monitorImage = new CroppedBitmap(screenshot, imageBounds);
            monitorImage.Freeze();
            ScreenImage.Source = monitorImage;

            // EvenOdd turns the selection into a "hole" in the dimmed layer.
            DimLayer.Data = new GeometryGroup
            {
                FillRule = FillRule.EvenOdd,
                Children = { _fullArea, _selectionArea }
            };

            SourceInitialized += (_, _) => PlaceOnMonitor();
            // Moving to a monitor with a different DPI makes WPF resize the window; restore the exact bounds afterwards.
            DpiChanged += (_, _) => Dispatcher.BeginInvoke(PlaceOnMonitor);
            SizeChanged += (_, _) => _fullArea.Rect = new Rect(ScreenImage.RenderSize);
            MouseEnter += (_, _) => Activate();
        }

        private void PlaceOnMonitor()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            NativeMethods.SetWindowPos(handle, NativeMethods.HWND_TOPMOST,
                _screenBounds.X, _screenBounds.Y, _screenBounds.Width, _screenBounds.Height,
                NativeMethods.SWP_NOACTIVATE);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _dragStart = GetClampedPosition(e);
            CaptureMouse();
            UpdateSelection(new Rect(_dragStart.Value, _dragStart.Value));
            SelectionBorder.Visibility = Visibility.Visible;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragStart is Point start)
            {
                UpdateSelection(new Rect(start, GetClampedPosition(e)));
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_dragStart is not Point start)
            {
                return;
            }

            _dragStart = null;
            ReleaseMouseCapture();

            var selection = new Rect(start, GetClampedPosition(e));
            if (selection.Width < MinSelectionSize || selection.Height < MinSelectionSize)
            {
                UpdateSelection(Rect.Empty);
                SelectionBorder.Visibility = Visibility.Collapsed;
                return;
            }

            SelectionCompleted?.Invoke(ToImagePixels(selection));
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            SelectionCompleted?.Invoke(null);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SelectionCompleted?.Invoke(null);
            }
        }

        private Point GetClampedPosition(MouseEventArgs e)
        {
            // While the mouse is captured, the pointer can leave the window.
            Point p = e.GetPosition(ScreenImage);
            return new Point(
                Math.Clamp(p.X, 0, ScreenImage.ActualWidth),
                Math.Clamp(p.Y, 0, ScreenImage.ActualHeight));
        }

        private void UpdateSelection(Rect selection)
        {
            _selectionArea.Rect = selection;

            if (!selection.IsEmpty)
            {
                Canvas.SetLeft(SelectionBorder, selection.X);
                Canvas.SetTop(SelectionBorder, selection.Y);
                SelectionBorder.Width = selection.Width;
                SelectionBorder.Height = selection.Height;
            }
        }

        // Converts a selection in WPF units to physical pixels inside the full screenshot.
        private Int32Rect ToImagePixels(Rect selection)
        {
            double scaleX = _imageBounds.Width / ScreenImage.ActualWidth;
            double scaleY = _imageBounds.Height / ScreenImage.ActualHeight;

            int x = (int)Math.Round(selection.X * scaleX);
            int y = (int)Math.Round(selection.Y * scaleY);
            int width = Math.Clamp((int)Math.Round(selection.Width * scaleX), 1, _imageBounds.Width - x);
            int height = Math.Clamp((int)Math.Round(selection.Height * scaleY), 1, _imageBounds.Height - y);

            return new Int32Rect(_imageBounds.X + x, _imageBounds.Y + y, width, height);
        }
    }
}
