namespace ControlMareas.App.ViewModels;

public sealed record KpiCardViewModel(
    string Label,
    string Value,
    string Delta,
    string Footnote);
