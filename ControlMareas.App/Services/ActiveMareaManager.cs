using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models;

namespace ControlMareas.App.Services;

public sealed class ActiveMareaManager : IActiveMareaManager
{
    private readonly IUserSettingsService _settingsService;
    private readonly IMareaService _mareaService;
    private string? _activeMareaId;
    private Marea? _activeMarea;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ActiveMareaManager(IUserSettingsService settingsService, IMareaService mareaService)
    {
        _settingsService = settingsService;
        _mareaService = mareaService;
    }

    public string? ActiveMareaId
    {
        get => _activeMareaId;
        private set
        {
            if (_activeMareaId != value)
            {
                _activeMareaId = value;
                OnPropertyChanged();
            }
        }
    }

    public Marea? ActiveMarea
    {
        get => _activeMarea;
        private set
        {
            if (_activeMarea != value)
            {
                _activeMarea = value;
                OnPropertyChanged();
            }
        }
    }

    public async Task InitializeAsync()
    {
        var settings = _settingsService.GetSettings();
        if (!string.IsNullOrEmpty(settings.ActiveMareaId))
        {
            await SetActiveMareaAsync(settings.ActiveMareaId);
        }
    }

    public async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(ActiveMareaId))
        {
            await SetActiveMareaAsync(ActiveMareaId);
        }
    }

    public async Task SetActiveMareaAsync(string? mareaId)
    {
        ActiveMareaId = mareaId;
        
        if (string.IsNullOrEmpty(mareaId))
        {
            ActiveMarea = null;
        }
        else
        {
            ActiveMarea = await _mareaService.GetMareaAsync(mareaId);
            // Si por alguna razón la marea ya no existe en la DB, limpiamos el ID
            if (ActiveMarea == null)
            {
                ActiveMareaId = null;
            }
        }

        _settingsService.UpdateSettings(s => s.ActiveMareaId = ActiveMareaId);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
