using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Controls;

public partial class SpeciesSelectorPremium : UserControl
{
    private bool _isInternalChange;

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
            if (SpeciesCombo.Template.FindName("PART_EditableTextBox", SpeciesCombo) is TextBox textBox)
            {
                textBox.TextChanged += TextBox_TextChanged;
            }
        };
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeciesSelectorPremium control)
        {
            if (e.NewValue != null)
            {
                // Crear una vista de colección privada para este control para evitar interferencias
                var cvs = new CollectionViewSource { Source = e.NewValue };
                var view = cvs.View;

                // Ordenamiento por defecto: Frecuentes primero, luego Nombre Vulgar
                view.SortDescriptions.Add(new SortDescription("Frecuente", ListSortDirection.Descending));
                view.SortDescriptions.Add(new SortDescription("NombreVulgar", ListSortDirection.Ascending));

                control._isInternalChange = true;
                control.SpeciesCombo.ItemsSource = view;
                control.SpeciesCombo.SelectedIndex = -1;
                control.SpeciesCombo.Text = string.Empty;
                control._isInternalChange = false;
            }
            else
            {
                control.SpeciesCombo.ItemsSource = null;
            }
        }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeciesSelectorPremium control)
        {
            if (control.SpeciesCombo.SelectedItem != e.NewValue)
            {
                control._isInternalChange = true;
                try
                {
                    // Limpiar el filtro antes de cambiar la selección para asegurar que el item sea visible
                    if (control.SpeciesCombo.ItemsSource is ICollectionView view)
                    {
                        view.Filter = null;
                    }

                    control.SpeciesCombo.SelectedItem = e.NewValue;

                    // Forzar actualización del texto si es necesario (cuando el combo es editable)
                    if (e.NewValue is Especie especie)
                    {
                        control.SpeciesCombo.Text = especie.FullDisplayName;
                    }
                    else if (e.NewValue == null)
                    {
                        control.SpeciesCombo.Text = string.Empty;
                    }
                }
                finally
                {
                    control._isInternalChange = false;
                }
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
    }

    private void SpeciesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInternalChange) return;
        SelectedItem = SpeciesCombo.SelectedItem;
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        SelectedItem = null;
        SpeciesCombo.Text = string.Empty;
        if (SpeciesCombo.ItemsSource is ICollectionView view)
        {
            view.Filter = null;
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInternalChange) return;

        var tb = (TextBox)sender;
        string filter = tb.Text;

        // Si el texto está vacío, limpiamos la selección
        if (string.IsNullOrEmpty(filter))
        {
            _isInternalChange = true;
            SelectedItem = null;
            SpeciesCombo.SelectedItem = null;
            _isInternalChange = false;
            
            if (SpeciesCombo.ItemsSource is ICollectionView viewNull)
                viewNull.Filter = null;
            return;
        }

        // Si el texto coincide exactamente con el elemento seleccionado, no filtramos (evita bucles)
        if (SpeciesCombo.SelectedItem is Especie sel && 
            string.Equals(sel.FullDisplayName, filter, System.StringComparison.OrdinalIgnoreCase))
            return;

        if (SpeciesCombo.ItemsSource is not ICollectionView view) return;

        view.Filter = item =>
        {
            if (item is Especie especie)
            {
                // Búsqueda por nombre vulgar o científico
                bool matches = (especie.NombreVulgar?.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (especie.NombreCientifico?.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ?? false);
                return matches;
            }
            return false;
        };
        
        if (view.IsEmpty)
        {
            SpeciesCombo.IsDropDownOpen = false;
        }
        else
        {
            SpeciesCombo.IsDropDownOpen = true;
        }
    }
}
