using System.Windows.Controls;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Views;

public partial class LanceEditView : UserControl
{
    public LanceEditView()
    {
        InitializeComponent();
    }

    private void CerrarEdicion_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LanceEditViewModel vm)
        {
            vm.SelectedCatchItem = null;
        }
    }
}
