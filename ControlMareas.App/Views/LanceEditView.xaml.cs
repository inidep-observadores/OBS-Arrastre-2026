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
}
