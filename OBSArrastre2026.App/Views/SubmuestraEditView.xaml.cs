using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Views;

public partial class SubmuestraEditView : UserControl
{
    public SubmuestraEditView()
    {
        InitializeComponent();
        this.DataContextChanged += SubmuestraEditView_DataContextChanged;
    }

    private void SubmuestraEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SubmuestraEditViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SubmuestraEditViewModel.SelectedSubmuestra))
        {
            // Pequeño delay para asegurar que el control esté visible y listo para el foco
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

    private void LateralForm_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            var element = Keyboard.FocusedElement as UIElement;
            if (element is TextBox)
            {
                e.Handled = true;
                element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}
