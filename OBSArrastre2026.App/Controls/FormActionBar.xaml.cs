using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace OBSArrastre2026.App.Controls
{
    /// <summary>
    /// Control estandarizado para la barra de acciones de los formularios (Cerrar/Guardar).
    /// </summary>
    [ContentProperty(nameof(AdditionalContent))]
    public partial class FormActionBar : UserControl
    {
        public FormActionBar()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty SaveCommandProperty =
            DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(FormActionBar), new PropertyMetadata(null));

        public ICommand SaveCommand
        {
            get => (ICommand)GetValue(SaveCommandProperty);
            set => SetValue(SaveCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(FormActionBar), new PropertyMetadata(null));

        public ICommand CancelCommand
        {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty SaveContentProperty =
            DependencyProperty.Register(nameof(SaveContent), typeof(string), typeof(FormActionBar), new PropertyMetadata("Guardar"));

        public string SaveContent
        {
            get => (string)GetValue(SaveContentProperty);
            set => SetValue(SaveContentProperty, value);
        }

        public static readonly DependencyProperty CancelContentProperty =
            DependencyProperty.Register(nameof(CancelContent), typeof(string), typeof(FormActionBar), new PropertyMetadata("Cerrar"));

        public string CancelContent
        {
            get => (string)GetValue(CancelContentProperty);
            set => SetValue(CancelContentProperty, value);
        }

        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register(nameof(AdditionalContent), typeof(object), typeof(FormActionBar), new PropertyMetadata(null));

        public object AdditionalContent
        {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        #endregion
    }
}
