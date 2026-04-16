namespace OBSArrastre2026.App.ViewModels;

public sealed record BuqueListItemViewModel(
    string ID,
    string Nombre,
    int Matricula,
    int IdRadial,
    int? IMO,
    int? MMSI);
