using CommunityToolkit.Mvvm.ComponentModel;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.ViewModels;

public sealed class NavigationItemViewModel : ObservableObject
{
    private bool _isEnabled = true;
    private bool _isSelected;

    public NavigationItemViewModel(NavigationSection section, string title, string caption, string glyph, bool requiresActiveMarea = false)
    {
        Section = section;
        Title = title;
        Caption = caption;
        Glyph = glyph;
        RequiresActiveMarea = requiresActiveMarea;
    }

    public NavigationSection Section { get; }
    public string Title { get; }
    public string Caption { get; }
    public string Glyph { get; }
    public bool RequiresActiveMarea { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
