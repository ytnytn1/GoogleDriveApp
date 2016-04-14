using System.Windows;
using System.Windows.Input;
using ViewModel;

namespace MainView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {       
        public MainWindow()
        {
            
            DataContext = new MainViewModel();
            InitializeComponent();           
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
