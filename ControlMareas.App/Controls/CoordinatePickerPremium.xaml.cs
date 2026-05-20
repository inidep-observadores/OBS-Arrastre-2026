using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ControlMareas.App.Controls;

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

    public static readonly DependencyProperty EsCompactoProperty =
        DependencyProperty.Register(nameof(EsCompacto), typeof(bool), typeof(CoordinatePickerPremium),
            new PropertyMetadata(false));

    public bool EsCompacto
    {
        get => (bool)GetValue(EsCompactoProperty);
        set => SetValue(EsCompactoProperty, value);
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
        int caretBefore = CoordInput.CaretIndex;
        
        // Contar cuántos dígitos hay antes del cursor actual para reposicionarlo luego
        int digitsBeforeCaret = text.Substring(0, Math.Min(caretBefore, text.Length)).Count(char.IsDigit);
        bool lastWasDigit = caretBefore > 0 && char.IsDigit(text[caretBefore - 1]);

        // Extraer solo dígitos
        string digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length > 5) digits = digits.Substring(0, 5);

        // Determinar cuadrante
        char? quad = null;
        string validQuads = IsLatitude ? "SN" : "OE";
        foreach (char c in text)
        {
            if (validQuads.Contains(c)) quad = c;
        }
        if (quad == null) quad = IsLatitude ? 'S' : 'O';

        // Construir texto formateado según la cantidad de dígitos
        string formatted = "";
        if (digits.Length > 0)
        {
            formatted += digits.Substring(0, Math.Min(digits.Length, 2));
            if (digits.Length >= 2)
            {
                formatted += "º";
                if (digits.Length > 2)
                {
                    formatted += digits.Substring(2, Math.Min(digits.Length - 2, 2));
                    if (digits.Length >= 4)
                    {
                        formatted += ",";
                        if (digits.Length > 4)
                        {
                            formatted += digits.Substring(4, 1);
                            formatted += "'" + quad;
                        }
                    }
                }
            }
        }

        if (text != formatted)
        {
            _isUpdating = true;
            CoordInput.Text = formatted;
            
            // Reposicionar cursor basándose en la cantidad de dígitos que había antes
            int newCaret = 0;
            int digitsCount = 0;
            while (newCaret < formatted.Length && digitsCount < digitsBeforeCaret)
            {
                if (char.IsDigit(formatted[newCaret])) digitsCount++;
                newCaret++;
            }
            
            // Si el usuario acaba de escribir un dígito y quedó parado justo antes de un símbolo de máscara, saltarlo
            if (lastWasDigit)
            {
                while (newCaret < formatted.Length && !char.IsDigit(formatted[newCaret]) && !validQuads.Contains(formatted[newCaret]))
                {
                    newCaret++;
                }
            }

            CoordInput.CaretIndex = newCaret;
            _isUpdating = false;
        }
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
