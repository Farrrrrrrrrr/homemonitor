using Avalonia.Controls;
using homemonitor.ViewModel;

namespace homemonitor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Explicitly set the DataContext
        DataContext = new MainViewModel();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Dispose();
        }

        base.OnClosed(e);
    }
}