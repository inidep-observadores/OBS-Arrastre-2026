using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.ViewModels;

public sealed record NavigationItemViewModel(
    NavigationSection Section,
    string Title,
    string Caption,
    string Glyph);
