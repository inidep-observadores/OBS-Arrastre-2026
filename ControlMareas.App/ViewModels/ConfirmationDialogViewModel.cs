using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ControlMareas.App.ViewModels;

public sealed partial class ConfirmationDialogViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private readonly Action<bool?> _onResult;
    private bool _showNoButton;

    public ConfirmationDialogViewModel(string title, string message, Action<bool?> onResult, bool showNoButton = false)
    {
        _title = title;
        _message = message;
        _onResult = onResult;
        _showNoButton = showNoButton;
        
        ConfirmCommand = new RelayCommand(() => _onResult(true));
        NoCommand = new RelayCommand(() => _onResult(false));
        CancelCommand = new RelayCommand(() => _onResult(null));
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public bool ShowNoButton
    {
        get => _showNoButton;
        set => SetProperty(ref _showNoButton, value);
    }

    public ICommand ConfirmCommand { get; }
    public ICommand NoCommand { get; }
    public ICommand CancelCommand { get; }
}
