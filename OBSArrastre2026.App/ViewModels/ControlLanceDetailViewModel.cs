using CommunityToolkit.Mvvm.ComponentModel;

namespace OBSArrastre2026.App.ViewModels;

public sealed class ControlLanceDetailViewModel : ObservableObject
{
    public int NroLance { get; set; }
    public double CapturaKg { get; set; }
    public double DescarteKg { get; set; }
    public double NetaKg => CapturaKg - DescarteKg;

    public string NroLanceDisplay => $"Lance {NroLance}";
    public string CapturaKgDisplay => CapturaKg.ToString("N1");
    public string DescarteKgDisplay => DescarteKg.ToString("N1");
    public string NetaKgDisplay => NetaKg.ToString("N1");
}
