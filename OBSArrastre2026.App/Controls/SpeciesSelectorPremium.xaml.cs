using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Controls;

public partial class SpeciesSelectorPremium : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SpeciesSelectorPremium), new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SpeciesSelectorPremium), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SpeciesSelectorPremium), new PropertyMetadata("Especie", OnLabelChanged));

    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(SpeciesSelectorPremium), new PropertyMetadata("Seleccione una especie...", OnWatermarkChanged));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Watermark
    {
        get => (string)GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public SpeciesSelectorPremium()
    {
        InitializeComponent();
        SpeciesCombo.SelectionChanged += SpeciesCombo_SelectionChanged;
        
        // Habilitar búsqueda incremental avanzada
        SpeciesCombo.Loaded += (s, e) =>
        {
            var textBox = SpeciesCombo.Template.FindName("PART_EditableTextBox", SpeciesCombo) as TextBox;
            if (textBox != null)
            {
                textBox.TextChanged += TextBox_TextChanged;
            }
        };
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeciesSelectorPremium control)
        {
            control.SpeciesCombo.ItemsSource = control.ItemsSource;
        }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeciesSelectorPremium control)
        {
            if (control.SpeciesCombo.SelectedItem != e.NewValue)
            {
                control.SpeciesCombo.SelectedItem = e.NewValue;
            }
        }
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeciesSelectorPremium control)
        {
            control.LabelText.Text = (string)e.NewValue;
        }
    }

    private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // El estilo PremiumComboBoxStyle debería manejar el Watermark si se bindea correctamente.
        // Por simplicidad en este control, asumimos que el estilo ya lo hace o lo implementaremos en el XAML del ComboBox si es necesario.
    }

    private void SpeciesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItem = SpeciesCombo.SelectedItem;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tb = (TextBox)sender;
        string filter = tb.Text;

        // Si el texto coincide exactamente con el elemento seleccionado, no filtramos (evita bucles)
        if (SpeciesCombo.SelectedItem is Especie sel && sel.FullDisplayName == filter)
            return;

        ICollectionView view = CollectionViewSource.GetDefaultView(SpeciesCombo.ItemsSource);
        if (view == null) return;

        if (string.IsNullOrWhiteSpace(filter))
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = item =>
            {
                if (item is Especie especie)
                {
                    return (especie.NombreVulgar?.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (especie.NombreCientifico?.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ?? false);
                }
                return false;
            };
        }
        
        SpeciesCombo.IsDropDownOpen = true;
    }
}
