using System.Windows.Controls;
using System.Windows;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Views;

public partial class LanceEditView : UserControl
{
    public LanceEditView()
    {
        InitializeComponent();
    }

    private void EspecieComboBox_DropDownOpened(object sender, System.EventArgs e)
    {
        if (sender is ComboBox cb)
        {
            var textBox = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (textBox != null)
            {
                // Usamos BeginInvoke para asegurar que la deselección ocurra DESPUÉS 
                // de que WPF ejecute su lógica interna de "seleccionar todo" al abrir el dropdown.
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    textBox.SelectionLength = 0;
                    textBox.CaretIndex = textBox.Text.Length;
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }

    private void CerrarEdicion_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LanceEditViewModel vm)
        {
            vm.SelectedCatchItem = null;
        }
    }
}
