using LibraryWPF.ViewModel;
using System.Windows;


namespace LibraryWPF.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new LibraryViewModel();
        }
    }
}
