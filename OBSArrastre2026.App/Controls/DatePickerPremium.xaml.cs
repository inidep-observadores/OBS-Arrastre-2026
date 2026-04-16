using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OBSArrastre2026.App.Controls;

public partial class DatePickerPremium : UserControl
{
    private bool _isUpdating;
    private const string DateFormat = "dd/MM/yyyy";

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(DatePickerPremium),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DatePickerPremium()
    {
        InitializeComponent();
        CalendarControl.SelectedDatesChanged += (s, e) => 
        {
            CalendarPopup.IsOpen = false;
            UpdateTextFromDate();
        };
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DatePickerPremium)d;
        if (!control._isUpdating)
        {
            control.UpdateTextFromDate();
        }
    }

    private void UpdateTextFromDate()
    {
        if (_isUpdating) return;
        _isUpdating = true;
        
        DateInput.Text = SelectedDate?.ToString(DateFormat) ?? string.Empty;
        ValidateDate(DateInput.Text);
        
        _isUpdating = false;
    }

    private void DateInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        var textBox = (TextBox)sender;
        var text = textBox.Text;

        // Lógica de máscara simple: auto-insertar '/'
        if (text.Length == 2 || text.Length == 5)
        {
            if (e.Changes.Any(c => c.AddedLength > 0))
            {
                if (!text.EndsWith("/"))
                {
                    textBox.Text = text + "/";
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        ValidateDate(textBox.Text);
        UpdateDateFromText(textBox.Text);
    }

    private void DateInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Bloquear teclas no deseadas, pero permitir navegación y control
        bool isDigit = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
        bool isNavigation = e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down ||
                            e.Key == Key.Home || e.Key == Key.End || e.Key == Key.PageUp || e.Key == Key.PageDown;
        bool isAction = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || e.Key == Key.Enter || e.Key == Key.Escape;

        if (!isDigit && !isNavigation && !isAction)
        {
            e.Handled = true;
        }
    }

    private void UpdateDateFromText(string text)
    {
        if (DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            _isUpdating = true;
            SelectedDate = date;
            _isUpdating = false;
        }
    }

    private void ValidateDate(string text)
    {
        // Ya no manipulamos MainBorder.BorderBrush aquí para no romper el estilo visual de foco en el XAML.
        // En el futuro, podríamos exponer una propiedad IsValid para que el XAML reaccione.
    }

    private void DateInput_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DateInput.Text))
        {
            SelectedDate = null;
            return;
        }

        if (!DateTime.TryParseExact(DateInput.Text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            UpdateTextFromDate(); // Revertir a la fecha válida anterior o vacío
        }
    }

    private void UserControl_GotFocus(object sender, RoutedEventArgs e)
    {
        DateInput.Focus();
    }
}
