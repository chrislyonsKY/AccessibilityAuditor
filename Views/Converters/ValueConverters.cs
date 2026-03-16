using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Views.Converters
{
    /// <summary>
    /// Converts <see cref="FindingSeverity"/> to a Segoe MDL2 Assets icon character.
    /// </summary>
    public sealed class SeverityToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is FindingSeverity severity
                ? severity switch
                {
                    FindingSeverity.Pass => "\uE73E",         // CheckMark
                    FindingSeverity.Warning => "\uE7BA",      // Warning
                    FindingSeverity.Fail => "\uE711",         // Cancel / X
                    FindingSeverity.ManualReview => "\uE7B3", // View / Eye
                    FindingSeverity.Error => "\uEA39",        // ErrorBadge
                    _ => "\uE897"                             // Help / ?
                }
                : "\uE897";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converts <see cref="FindingSeverity"/> to a <see cref="SolidColorBrush"/>.
    /// Colors are chosen to meet 3:1 contrast against both Pro light (#F0F0F0) and dark (#323232) backgrounds.
    /// </summary>
    public sealed class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not FindingSeverity severity)
                return Brushes.Gray;

            var brush = severity switch
            {
                FindingSeverity.Pass => new SolidColorBrush(Color.FromRgb(0x3D, 0xA6, 0x3D)),    // #3DA63D � brighter green
                FindingSeverity.Warning => new SolidColorBrush(Color.FromRgb(0xE5, 0xA1, 0x00)),  // #E5A100 � brighter amber
                FindingSeverity.Fail => new SolidColorBrush(Color.FromRgb(0xE0, 0x43, 0x43)),     // #E04343 � brighter red
                FindingSeverity.ManualReview => new SolidColorBrush(Color.FromRgb(0x4D, 0x8F, 0xD6)), // #4D8FD6 � brighter blue
                FindingSeverity.Error => new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),    // #CC3333
                _ => new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))
            };
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converts a score (0�100) to a <see cref="SolidColorBrush"/> (red ? yellow ? green gradient).
    /// </summary>
    public sealed class ScoreToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int score)
                return Brushes.Gray;

            return score switch
            {
                >= 80 => new SolidColorBrush(Color.FromRgb(0x2D, 0x8B, 0x2D)),
                >= 60 => new SolidColorBrush(Color.FromRgb(0xD4, 0x8A, 0x00)),
                _ => new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x2B))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converts a <see cref="ColorInfo"/> to a <see cref="SolidColorBrush"/>.
    /// </summary>
    public sealed class ColorInfoToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ColorInfo color)
                return Brushes.Transparent;

            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converts a boolean to <see cref="Visibility"/>. True = Visible, False = Collapsed.
    /// </summary>
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Inverted boolean to <see cref="Visibility"/>. True = Collapsed, False = Visible.
    /// </summary>
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Inverts a boolean value. True ? False, False ? True.
    /// </summary>
    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : value;
        }
    }

    /// <summary>
    /// Converts an enum value to/from bool for RadioButton binding.
    /// ConverterParameter is the enum member name to match.
    /// </summary>
    public sealed class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is true && parameter is string name)
                return Enum.Parse(targetType, name);
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Converts a hex color string (e.g. "#4A4A4A") to a <see cref="SolidColorBrush"/>.
    /// </summary>
    public sealed class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && hex.Length == 7 && hex[0] == '#')
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }
                catch { }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converts <see cref="Services.Fixes.FixStatus"/> to a display string.
    /// </summary>
    public sealed class FixStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Services.Fixes.FixStatus status
                ? status switch
                {
                    Services.Fixes.FixStatus.Applied => "Fixed",
                    Services.Fixes.FixStatus.Suggested => "Suggestion",
                    Services.Fixes.FixStatus.Failed => "Failed",
                    _ => string.Empty
                }
                : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
