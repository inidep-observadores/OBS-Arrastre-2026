using System.Windows;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
