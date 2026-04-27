using System.Windows.Controls;

namespace OBSArrastre2026.App.Views;

public partial class MuestraEditView : UserControl
{
    public MuestraEditView()
    {
        InitializeComponent();
        DataContextChanged += MuestraEditView_DataContextChanged;
    }

    private void MuestraEditView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.MuestraEditViewModel oldVm)
        {
            oldVm.FrecuenciasTallas.CollectionChanged -= FrecuenciasTallas_CollectionChanged;
        }
        if (e.NewValue is ViewModels.MuestraEditViewModel newVm)
        {
            newVm.FrecuenciasTallas.CollectionChanged += FrecuenciasTallas_CollectionChanged;
        }
    }

    private void FrecuenciasTallas_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            var newItem = e.NewItems?[0];
            if (newItem != null)
            {
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    FrecuenciasGrid.SelectedItem = newItem;
                    FrecuenciasGrid.ScrollIntoView(newItem);
                    
                    // Intentar dar foco a la primera celda del nuevo item
                    FrecuenciasGrid.UpdateLayout();
                    var row = (DataGridRow)FrecuenciasGrid.ItemContainerGenerator.ContainerFromItem(newItem);
                    if (row != null)
                    {
                        var cell = FrecuenciasGrid.Columns[0].GetCellContent(row)?.Parent as DataGridCell;
                        cell?.Focus();
                        FrecuenciasGrid.BeginEdit();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }

    private void EspecieComboBox_DropDownOpened(object sender, System.EventArgs e)
    {
        if (sender is ComboBox cb)
        {
            var textBox = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (textBox != null)
            {
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    textBox.SelectionLength = 0;
                    textBox.CaretIndex = textBox.Text.Length;
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }
}
