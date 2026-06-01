using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace ControlMareas.App.ViewModels;

public sealed partial class RevisorDialogViewModel : ObservableObject
{
    private string _nombre = string.Empty;
    private string _apellido = string.Empty;
    private readonly Action<string, string> _onAccept;

    private string? _errorMessage = null;

    public RevisorDialogViewModel(Action<string, string> onAccept)
    {
        _onAccept = onAccept;
        AcceptCommand = new RelayCommand(OnAccept);
    }

    public string Nombre
    {
        get => _nombre;
        set
        {
            if (SetProperty(ref _nombre, value))
                ErrorMessage = null;
        }
    }

    public string Apellido
    {
        get => _apellido;
        set
        {
            if (SetProperty(ref _apellido, value))
                ErrorMessage = null;
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand AcceptCommand { get; }

    private void OnAccept()
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido))
        {
            ErrorMessage = "Ambos campos son obligatorios.";
            return;
        }

        _onAccept(Nombre.Trim(), Apellido.Trim());
    }
}
