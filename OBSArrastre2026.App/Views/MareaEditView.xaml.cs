using System.Windows;
using System.Windows.Controls;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Views;

public partial class MareaEditView : UserControl
{
    public MareaEditView()
    {
        InitializeComponent();
        Loaded += (s, e) => AnioInput.Focus();
    }

    private void EtapaDatePicker_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MareaEtapaItemViewModel vm)
        {
            // Solo enfocamos si la etapa está expandida (la nueva se crea expandida)
            if (vm.IsExpanded)
            {
                fe.Focus();
            }
        }
    }
}
