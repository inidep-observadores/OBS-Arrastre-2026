using System.Windows;
using OBSArrastre2026.App.ViewModels;
using OBSArrastre2026.App.Services;
using System;

namespace OBSArrastre2026.App;

public partial class MainWindow : Window
{
    private readonly IUserSettingsService _settingsService;

    public MainWindow(MainWindowViewModel viewModel, IUserSettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;

        StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // Solo persistimos si el estado no es minimizado para no perder el estado anterior real
        if (WindowState != WindowState.Minimized)
        {
            _settingsService.UpdateSettings(s => s.WindowState = WindowState);
        }
    }
}
