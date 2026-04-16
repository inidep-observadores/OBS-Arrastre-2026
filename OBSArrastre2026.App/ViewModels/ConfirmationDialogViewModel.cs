using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace OBSArrastre2026.App.ViewModels;

public sealed partial class ConfirmationDialogViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private readonly Action<bool> _onResult;

    public ConfirmationDialogViewModel(string title, string message, Action<bool> onResult)
    {
        _title = title;
        _message = message;
        _onResult = onResult;
        
        ConfirmCommand = new RelayCommand(() => _onResult(true));
        CancelCommand = new RelayCommand(() => _onResult(false));
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

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
}
