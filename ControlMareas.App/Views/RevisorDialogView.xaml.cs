using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ControlMareas.App.Views;

public partial class RevisorDialogView : UserControl
{
    public RevisorDialogView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            txtNombre.Focus();
            Keyboard.Focus(txtNombre);
        };
    }
}
