using System.Windows;

namespace ServerWatcher
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            viewModel = new MainViewModel();
            DataContext = viewModel;
        }

        private async void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await viewModel.InitializeAsync();
        }

        private void Window_Closing(
            object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            viewModel.Stop();
        }
    }
}