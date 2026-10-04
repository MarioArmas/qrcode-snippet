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
            var decoder = new QrDecoderService();
            DataContext = new MainViewModel(
                new ClipboardService(decoder),
                new ImageFileService(),
                new ScreenCaptureService(),
                new LinkService(),
                decoder);
        }
    }
}