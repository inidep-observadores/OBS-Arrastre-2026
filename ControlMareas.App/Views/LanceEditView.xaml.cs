using System.Windows.Controls;
using System.Windows;
using ControlMareas.App.ViewModels;

namespace ControlMareas.App.Views;

public partial class LanceEditView : UserControl
{
    public LanceEditView()
    {
        InitializeComponent();
    }

    private void CerrarEdicion_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LanceEditViewModel vm)
        {
            vm.SelectedCatchItem = null;
        }
    }

    private async void RemoveButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (DataContext is LanceEditViewModel vm && sender is Button button)
        {
            if (button.DataContext is CatchItemViewModel catchItem)
            {
                var result = await (vm.ShowConfirmation?.Invoke("Eliminar especie",
                    $"¿Está seguro de que desea eliminar {catchItem.EspecieNombreVulgar}?") ?? Task.FromResult<bool?>(false));

                if (result == true)
                {
                    catchItem.RequestDeletion?.Invoke(catchItem);
                }
            }
        }
    }
}
