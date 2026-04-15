using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace OBSArrastre2026.App.ViewModels;

public enum MessageDialogType
{
    Info,
    Success,
    Warning,
    Error
}

public sealed partial class MessageDialogViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private MessageDialogType _type = MessageDialogType.Info;
    private readonly Action _onClose;

    public MessageDialogViewModel(string title, string message, MessageDialogType type, Action onClose)
    {
        _title = title;
        _message = message;
        _type = type;
        _onClose = onClose;
        CloseCommand = new RelayCommand(onClose);
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

    public MessageDialogType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public ICommand CloseCommand { get; }
}
