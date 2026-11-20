using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QrSnippet.Services;
using QrSnippet.ViewModels;

namespace QrSnippet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(
                new ClipboardService(),
                new ImageFileService(),
                new ScreenCaptureService(),
                new LinkService(),
                new QrDecoderService());
        }

        private MainViewModel ViewModel => (MainViewModel)DataContext;

        // Preview* events are used because the result TextBox would otherwise handle text drops itself.
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            bool canDrop = GetDroppedFile(e) is not null;
            e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
            DropHint.Visibility = canDrop ? Visibility.Visible : Visibility.Collapsed;
            e.Handled = true;
        }

        private void Window_PreviewDragLeave(object sender, DragEventArgs e)
        {
            DropHint.Visibility = Visibility.Collapsed;
        }

        private async void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            DropHint.Visibility = Visibility.Collapsed;
            e.Handled = true;

            if (GetDroppedFile(e) is string path)
            {
                Activate();
                await ViewModel.LoadFileAsync(path);
            }
        }

        // Only a single file is accepted; its content is validated when it is loaded.
        private static string? GetDroppedFile(DragEventArgs e)
        {
            return e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files ? files[0] : null;
        }
    }
}