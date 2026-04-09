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
        "#F5F7FB",
        "#FFFFFF",
        "#F8FAFC",
        "#E5E7EB",
        "#0F172A",
        "#475569",
        "#0B1220",
        "#FFFFFF",
        "#DCE7FF",
        "#2F6BFF",
        "#D8E4FF",
        "#EEF4FF",
        "#334155",
        "#E2E8F0");

    private static readonly ThemePalette DarkPalette = new(
        "#07111F",
        "#0F1A2B",
        "#132238",
        "#20314C",
        "#F8FAFC",
        "#94A3B8",
        "#09101D",
        "#0F1A2B",
        "#1A3E71",
        "#71A3FF",
        "#17396A",
        "#13284A",
        "#CBD5E1",
        "#20314C");

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
        Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
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
        string TableRowBorder);
}
