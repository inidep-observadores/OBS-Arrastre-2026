using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ControlMareas.App.Models.Reports;

namespace ControlMareas.App.ViewModels;

public class RecibiProyectoSelectionItem : ObservableObject
{
    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public RecibiProyectoEspecieItem EspecieItem { get; set; } = null!;
    public string NombreEspecie => EspecieItem.NombreEspecie;
    public string NombreCompleto => !string.IsNullOrEmpty(EspecieItem.NombreCientifico) 
        ? $"{NombreEspecie} ({EspecieItem.NombreCientifico})" 
        : NombreEspecie;
    public string DetalleLances => string.Join(", ", EspecieItem.Lances.Select(l => l.NroLance));
}

public class RecibiProyectoSelectionViewModel : ObservableObject
{
    private readonly TaskCompletionSource<List<RecibiProyectoEspecieItem>?> _tcs = new();
    public Task<List<RecibiProyectoEspecieItem>?> SelectionTask => _tcs.Task;

    public ObservableCollection<RecibiProyectoSelectionItem> Especies { get; } = new();

    public ICommand ConfirmarCommand { get; }
    public ICommand CancelarCommand { get; }

    public RecibiProyectoSelectionViewModel(List<RecibiProyectoEspecieItem> especies)
    {
        foreach (var e in especies)
        {
            Especies.Add(new RecibiProyectoSelectionItem { EspecieItem = e });
        }

        ConfirmarCommand = new RelayCommand(() => 
        {
            var selected = Especies.Where(x => x.IsSelected).Select(x => x.EspecieItem).ToList();
            _tcs.SetResult(selected);
        });

        CancelarCommand = new RelayCommand(() => _tcs.SetResult(null));
    }
}
