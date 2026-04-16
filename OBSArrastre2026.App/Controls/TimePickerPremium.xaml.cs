using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OBSArrastre2026.App.Controls;

public partial class TimePickerPremium : UserControl
{
    public static readonly DependencyProperty SelectedTimeProperty =
        DependencyProperty.Register(nameof(SelectedTime), typeof(TimeSpan?), typeof(TimePickerPremium),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeChanged));

    public TimeSpan? SelectedTime
    {
        get => (TimeSpan?)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public static readonly DependencyProperty EsCompactoProperty =
        DependencyProperty.Register(nameof(EsCompacto), typeof(bool), typeof(TimePickerPremium),
            new PropertyMetadata(false));

    public bool EsCompacto
    {
        get => (bool)GetValue(EsCompactoProperty);
        set => SetValue(EsCompactoProperty, value);
    }

    private bool _isUpdating;

    public TimePickerPremium()
    {
        InitializeComponent();
    }

    private void UserControl_GotFocus(object sender, RoutedEventArgs e)
    {
        TimeInput.Focus();
    }

    private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimePickerPremium control && !control._isUpdating)
        {
            control.UpdateTimeString((TimeSpan?)e.NewValue);
        }
    }

    private void UpdateTimeString(TimeSpan? time)
    {
        _isUpdating = true;
        if (time == null)
        {
            TimeInput.Text = string.Empty;
        }
        else
        {
            TimeInput.Text = $"{(int)time.Value.TotalHours:D2}:{time.Value.Minutes:D2}";
        }
        _isUpdating = false;
    }

    private void TimeInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        string text = TimeInput.Text;
        
        // Impedir entrada de caracteres no numéricos excepto el separador
        string filtered = new string(text.Where(c => char.IsDigit(c) || c == ':').ToArray());
        if (text != filtered)
        {
            _isUpdating = true;
            int caret = TimeInput.CaretIndex;
            TimeInput.Text = filtered;
            TimeInput.CaretIndex = Math.Max(0, caret - 1);
            _isUpdating = false;
            return;
        }

        // Auto-insertar el separador ":" al escribir el segundo dígito
        if (filtered.Length == 2 && !filtered.Contains(":"))
        {
            _isUpdating = true;
            TimeInput.Text = filtered + ":";
            TimeInput.CaretIndex = 3;
            _isUpdating = false;
        }
    }

    private void TimeInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            e.Handled = true;
            AdjustTime(e.Key == Key.Up ? 1 : -1);
        }
    }

    private void AdjustTime(int minutes)
    {
        var current = SelectedTime ?? TimeSpan.Zero;
        try
        {
            var newTime = current.Add(TimeSpan.FromMinutes(minutes));
            
            // Ciclo de 24 horas
            if (newTime.TotalDays >= 1) newTime = newTime.Subtract(TimeSpan.FromDays(1));
            if (newTime.TotalDays < 0) newTime = newTime.Add(TimeSpan.FromDays(1));

            UpdateSelectedTime(newTime);
        }
        catch { /* Ignorar errores de rango extremo */ }
    }

    private void UpdateSelectedTime(TimeSpan? time)
    {
        _isUpdating = true;
        SelectedTime = time;
        UpdateTimeString(time);
        _isUpdating = false;
    }

    private void TimeInput_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TimeInput.Text))
        {
            UpdateSelectedTime(null);
            return;
        }

        string raw = TimeInput.Text;
        if (TimeSpan.TryParse(raw, out var parsedTime) && parsedTime.TotalDays < 1)
        {
            UpdateSelectedTime(parsedTime);
        }
        else
        {
            // Lógica de recuperación para entradas sin separador (ej: "0930" -> 09:30)
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length >= 3 && digits.Length <= 4)
            {
                string hStr = digits.Length == 3 ? digits.Substring(0, 1) : digits.Substring(0, 2);
                string mStr = digits.Length == 3 ? digits.Substring(1, 2) : digits.Substring(2, 2);

                if (int.TryParse(hStr, out int h) && int.TryParse(mStr, out int m) && h < 24 && m < 60)
                {
                    UpdateSelectedTime(new TimeSpan(h, m, 0));
                    return;
                }
            }

            // Si es inválido, intentamos quedarnos con lo que había o limpiar
            UpdateTimeString(SelectedTime);
        }
    }
}
