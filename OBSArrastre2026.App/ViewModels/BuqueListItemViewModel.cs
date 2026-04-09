namespace OBSArrastre2026.App.ViewModels;

public sealed record BuqueListItemViewModel(
    string Nombre,
    int Matricula,
    int IdRadial,
    int? IMO,
    int? MMSI);
