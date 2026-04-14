using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.Services;

public interface IThemeService
{
    AppThemeMode CurrentMode { get; }

    void ApplyTheme(AppThemeMode mode);
}

public sealed class ThemeService : IThemeService
{
    private static readonly ThemePalette LightPalette = new(
        ShellBackground:            "#F5F7FB",
        SidebarBackground:          "#FFFFFF",
        SidebarSecondaryBackground: "#F8FAFC",
        SidebarBorder:              "#E5E7EB",
        PrimaryText:                "#0F172A",
        SecondaryText:              "#475569",
        HeroBackground:             "#FFFFFF",
        CardBackground:             "#FFFFFF",
        CardBorder:                 "#DCE7FF",
        Accent:                     "#2F6BFF",
        AccentMuted:                "#D8E4FF",
        SelectionBackground:        "#EEF4FF",
        TableHeader:                "#EFF2F7",
        TableRowBorder:             "#E2E8F0",
        SuccessForeground:          "#15803D",
        SuccessBackground:          "#DCFCE7",
        WarningForeground:          "#B45309",
        WarningBackground:          "#FEF3C7",
        DangerForeground:           "#B91C1C",
        DangerBackground:           "#FEE2E2",
        HoverBackground:            "#F1F5F9",
        SurfaceBackground:          "#F8FAFC");

    private static readonly ThemePalette DarkPalette = new(
        ShellBackground:            "#07111F",
        SidebarBackground:          "#0F1A2B",
        SidebarSecondaryBackground: "#132238",
        SidebarBorder:              "#20314C",
        PrimaryText:                "#F8FAFC",
        SecondaryText:              "#94A3B8",
        HeroBackground:             "#09101D",
        CardBackground:             "#0F1A2B",
        CardBorder:                 "#1A3E71",
        Accent:                     "#71A3FF",
        AccentMuted:                "#17396A",
        SelectionBackground:        "#13284A",
        TableHeader:                "#0D1929",
        TableRowBorder:             "#20314C",
        SuccessForeground:          "#4ADE80",
        SuccessBackground:          "#052E16",
        WarningForeground:          "#FCD34D",
        WarningBackground:          "#451A03",
        DangerForeground:           "#F87171",
        DangerBackground:           "#450A0A",
        HoverBackground:            "#1E293B",
        SurfaceBackground:          "#0B1423");

    public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.System;

    public void ApplyTheme(AppThemeMode mode)
    {
        CurrentMode = mode;
        var palette = mode switch
        {
            AppThemeMode.Light => LightPalette,
            AppThemeMode.Dark => DarkPalette,
            _ => IsSystemLightTheme() ? LightPalette : DarkPalette
        };

        SetBrush("ShellBackgroundBrush", palette.ShellBackground);
        SetBrush("SidebarBackgroundBrush", palette.SidebarBackground);
        SetBrush("SidebarSecondaryBackgroundBrush", palette.SidebarSecondaryBackground);
        SetBrush("SidebarBorderBrush", palette.SidebarBorder);
        SetBrush("PrimaryTextBrush", palette.PrimaryText);
        SetBrush("SecondaryTextBrush", palette.SecondaryText);
        SetBrush("HeroBackgroundBrush", palette.HeroBackground);
        SetBrush("CardBackgroundBrush", palette.CardBackground);
        SetBrush("CardBorderBrush", palette.CardBorder);
        SetBrush("AccentBrush", palette.Accent);
        SetBrush("AccentMutedBrush", palette.AccentMuted);
        SetBrush("SelectionBackgroundBrush", palette.SelectionBackground);
        SetBrush("TableHeaderBrush", palette.TableHeader);
        SetBrush("TableRowBorderBrush", palette.TableRowBorder);
        SetBrush("SuccessForegroundBrush", palette.SuccessForeground);
        SetBrush("SuccessBackgroundBrush", palette.SuccessBackground);
        SetBrush("WarningForegroundBrush", palette.WarningForeground);
        SetBrush("WarningBackgroundBrush", palette.WarningBackground);
        SetBrush("DangerForegroundBrush", palette.DangerForeground);
        SetBrush("DangerBackgroundBrush", palette.DangerBackground);
        SetBrush("HoverBackgroundBrush", palette.HoverBackground);
        SetBrush("SurfaceBackgroundBrush", palette.SurfaceBackground);

        Application.Current.Resources["DisplayFontFamily"] = new FontFamily("Segoe UI Variable Display");
        Application.Current.Resources["TextFontFamily"] = new FontFamily("Segoe UI Variable Text");
    }

    private static bool IsSystemLightTheme()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
        var value = key?.GetValue("AppsUseLightTheme");
        return value is int flag ? flag > 0 : true;
    }

    private static void SetBrush(string key, string color)
    {
        var colorVal = (Color)ColorConverter.ConvertFromString(color);
        Application.Current.Resources[key] = new SolidColorBrush(colorVal);
        
        // Si es el AccentBrush, también registramos el AccentColor para que los bindings estáticos funcionen
        if (key == "AccentBrush")
        {
            Application.Current.Resources["AccentColor"] = colorVal;
        }
    }

    private sealed record ThemePalette(
        string ShellBackground,
        string SidebarBackground,
        string SidebarSecondaryBackground,
        string SidebarBorder,
        string PrimaryText,
        string SecondaryText,
        string HeroBackground,
        string CardBackground,
        string CardBorder,
        string Accent,
        string AccentMuted,
        string SelectionBackground,
        string TableHeader,
        string TableRowBorder,
        string SuccessForeground,
        string SuccessBackground,
        string WarningForeground,
        string WarningBackground,
        string DangerForeground,
        string DangerBackground,
        string HoverBackground,
        string SurfaceBackground);
}
