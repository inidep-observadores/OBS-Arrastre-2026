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
    private string? _details;
    private bool _isDetailsExpanded;
    private MessageDialogType _type = MessageDialogType.Info;
    private readonly Action _onClose;

    public MessageDialogViewModel(string title, string message, string? details, MessageDialogType type, Action onClose)
    {
        _title = title;
        _message = message;
        _details = details;
        _type = type;
        _onClose = onClose;
        CloseCommand = new RelayCommand(onClose);
        ToggleDetailsCommand = new RelayCommand(() => IsDetailsExpanded = !IsDetailsExpanded);
        CopyToClipboardCommand = new RelayCommand(OnCopyToClipboard);
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

    public string? Details
    {
        get => _details;
        set => SetProperty(ref _details, value);
    }

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set => SetProperty(ref _isDetailsExpanded, value);
    }

    public MessageDialogType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public ICommand CloseCommand { get; }
    public ICommand ToggleDetailsCommand { get; }
    public ICommand CopyToClipboardCommand { get; }

    private void OnCopyToClipboard()
    {
        if (!string.IsNullOrEmpty(Details))
        {
            System.Windows.Clipboard.SetText(Details);
        }
    }
}
