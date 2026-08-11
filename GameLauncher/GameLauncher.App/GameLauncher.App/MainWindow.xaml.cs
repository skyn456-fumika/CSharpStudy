using System.Windows;
using GameLauncher.App.ViewModels;

namespace GameLauncher.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = new MainViewModel();

            DataContext = viewModel;

            await viewModel.InitializeAsync();
        }
    }
}