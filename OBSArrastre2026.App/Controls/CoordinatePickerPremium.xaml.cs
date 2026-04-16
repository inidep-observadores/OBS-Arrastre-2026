using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OBSArrastre2026.App.Controls;

public partial class CoordinatePickerPremium : UserControl
{
    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(nameof(SelectedValue), typeof(double?), typeof(CoordinatePickerPremium),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

    public static readonly DependencyProperty IsLatitudeProperty =
        DependencyProperty.Register(nameof(IsLatitude), typeof(bool), typeof(CoordinatePickerPremium),
            new PropertyMetadata(true));

    public double? SelectedValue
    {
        get => (double?)GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public bool IsLatitude
    {
        get => (bool)GetValue(IsLatitudeProperty);
        set => SetValue(IsLatitudeProperty, value);
    }

    private bool _isUpdating;

    public CoordinatePickerPremium()
    {
        InitializeComponent();
    }

    private void UserControl_GotFocus(object sender, RoutedEventArgs e)
    {
        CoordInput.Focus();
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CoordinatePickerPremium control && !control._isUpdating)
        {
            control.UpdateInputString((double?)e.NewValue);
        }
    }

    private void UpdateInputString(double? value)
    {
        _isUpdating = true;
        CoordInput.Text = FormatToDms(value);
        _isUpdating = false;
    }

    private string FormatToDms(double? value)
    {
        if (value == null) return string.Empty;
        
        bool isNeg = value < 0;
        char quad = IsLatitude ? (isNeg ? 'S' : 'N') : (isNeg ? 'O' : 'E');
        
        double abs = Math.Abs(value.Value);
        int deg = (int)abs;
        double minTotal = (abs - deg) * 60;
        int min = (int)minTotal;
        int tenth = (int)Math.Round((minTotal - min) * 10);

        if (tenth >= 10) { tenth = 0; min++; }
        if (min >= 60) { min = 0; deg++; }

        return $"{deg}º{min:D2},{tenth}'{quad}";
    }

    private void CoordInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        string text = CoordInput.Text.ToUpper();
        
        // Impedir entrada de caracteres no válidos
        string filtered = new string(text.Where(c => char.IsDigit(c) || "º,.'SNOE".Contains(c)).ToArray());
        if (text != filtered)
        {
            _isUpdating = true;
            int caret = CoordInput.CaretIndex;
            CoordInput.Text = filtered;
            CoordInput.CaretIndex = Math.Max(0, caret - 1);
            _isUpdating = false;
            return;
        }

        // Auto-formateo simple al escribir (Detección de segmentos por longitud)
        // Ejemplo: 46150 -> 46º15,0'S (basado en cuadrante default)
        if (filtered.All(char.IsDigit) && filtered.Length >= 5)
        {
            TryFormatRawDigits(filtered);
        }
    }

    private void TryFormatRawDigits(string digits)
    {
        if (digits.Length < 5) return;

        try
        {
            int deg, min, tenth;
            if (IsLatitude || digits.Length == 5)
            {
                deg = int.Parse(digits.Substring(0, 2));
                min = int.Parse(digits.Substring(2, 2));
                tenth = int.Parse(digits.Substring(4, 1));
            }
            else // Longitude con 3 dígitos 
            {
                deg = int.Parse(digits.Substring(0, 3));
                min = int.Parse(digits.Substring(3, 2));
                tenth = int.Parse(digits.Substring(5, 1));
            }

            char quad = IsLatitude ? 'S' : 'O';
            _isUpdating = true;
            CoordInput.Text = $"{deg}º{min:D2},{tenth}'{quad}";
            CoordInput.CaretIndex = CoordInput.Text.Length;
            _isUpdating = false;
        }
        catch { }
    }

    private void CoordInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            e.Handled = true;
            AdjustCoord(e.Key == Key.Up ? 0.1 : -0.1); // Ajustar décimas de minuto
        }
    }

    private void AdjustCoord(double deltaMinutes)
    {
        double current = SelectedValue ?? 0;
        double sign = current < 0 ? -1 : 1;
        
        // El delta es en minutos, convertimos a grados decimales
        double deltaDegrees = deltaMinutes / 60.0;
        
        // Sumamos al valor absoluto para que arriba siempre aleje del ecuador/meridiano si es negativo?
        // No, sumamos al valor real para que sea consistente
        double newValue = current + (deltaDegrees * sign); 

        // Límites
        double max = IsLatitude ? 90 : 180;
        if (Math.Abs(newValue) > max) newValue = max * (newValue < 0 ? -1 : 1);

        UpdateSelectedValue(newValue);
    }

    private void UpdateSelectedValue(double? value)
    {
        _isUpdating = true;
        SelectedValue = value;
        CoordInput.Text = FormatToDms(value);
        _isUpdating = false;
    }

    private void CoordInput_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CoordInput.Text))
        {
            UpdateSelectedValue(null);
            return;
        }

        string raw = CoordInput.Text.ToUpper();
        
        // Regex para capturar segmentos: (Grados)º(Minutos),(Décimas)'(Cuadrante)
        var match = Regex.Match(raw, @"(\d+)[º°]?(\d{1,2})[,\.]?(\d)?['""]?([SNOE])?");
        
        if (match.Success)
        {
            try
            {
                int deg = int.Parse(match.Groups[1].Value);
                int min = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                int tenth = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                char quad = match.Groups[4].Success ? match.Groups[4].Value[0] : (IsLatitude ? 'S' : 'O');

                // Validaciones
                if (min >= 60) { deg += min / 60; min %= 60; }
                double max = IsLatitude ? 90 : 180;
                if (deg > max || (deg == max && (min > 0 || tenth > 0)))
                {
                    deg = (int)max; min = 0; tenth = 0;
                }

                double dec = deg + (min / 60.0) + (tenth / 600.0);
                if (quad == 'S' || quad == 'O') dec = -dec;

                UpdateSelectedValue(dec);
            }
            catch
            {
                UpdateInputString(SelectedValue);
            }
        }
        else
        {
            // Revertir
            UpdateInputString(SelectedValue);
        }
    }
}
