using System;
using System.ComponentModel;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IActiveMareaManager : INotifyPropertyChanged
{
    string? ActiveMareaId { get; }
    Marea? ActiveMarea { get; }
    
    Task SetActiveMareaAsync(string? mareaId);
    Task InitializeAsync();
}
