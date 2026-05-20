using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services;

public interface IActiveMareaManager : INotifyPropertyChanged
{
    string? ActiveMareaId { get; }
    Marea? ActiveMarea { get; }
    
    Task SetActiveMareaAsync(string? mareaId);
    Task RefreshAsync();
    Task InitializeAsync();
}
