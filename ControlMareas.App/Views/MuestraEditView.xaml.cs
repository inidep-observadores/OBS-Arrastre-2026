using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ControlMareas.App.ViewModels;

namespace ControlMareas.App.Views;

public partial class MuestraEditView : UserControl
{
    public MuestraEditView()
    {
        InitializeComponent();
        this.DataContextChanged += MuestraEditView_DataContextChanged;
    }

    private void MuestraEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MuestraEditViewModel oldVm)
            oldVm.PropertyChanged -= Vm_PropertyChanged;

        if (e.NewValue is MuestraEditViewModel newVm)
            newVm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MuestraEditViewModel.SelectedFrecuencia))
        {
            // Cuando cambia la frecuencia seleccionada, ponemos el foco en el primer campo del formulario lateral
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                var textBox = this.FindName("FirstField") as TextBox;
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
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

    private void LateralForm_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            var element = Keyboard.FocusedElement as UIElement;
            if (element is TextBox)
            {
                // Si estamos en un TextBox, el ENTER actúa como TAB para mover al siguiente campo
                e.Handled = true;
                element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}
