using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BlockchainNetworkAnalyzer.Converters
{
    /// <summary>
    /// Converters برای XAML Binding
    /// XAML Data Converters and Helpers
    /// </summary>

    /// <summary>
    /// تبدیل درصد اعتماد به رنگ - Confidence to Color Converter
    /// </summary>
    public class ConfidenceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), out double confidence))
            {
                if (confidence >= 0.8)
                    return new SolidColorBrush(Colors.Red);           // خطرناک
                else if (confidence >= 0.6)
                    return new SolidColorBrush(Colors.Orange);        // مریب
                else if (confidence >= 0.4)
                    return new SolidColorBrush(Colors.Yellow);        // مشکوک
                else
                    return new SolidColorBrush(Colors.Green);         // عادی
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // تبدیل رنگ به درصد اعتماد - معمولاً بلااستفاده پس مقدار پیش‌فرض
            return 0.0;
        }
    }

    /// <summary>
    /// تبدیل وضعیت به رنگ - Status to Color Converter
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLower() ?? "";
            return status switch
            {
                "online" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),      // سبز
                "offline" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),     // خاکستری
                "suspicious" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),  // نارنجی
                "infected" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),    // قرمز
                _ => new SolidColorBrush(Colors.Blue)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // تبدیل رنگ به وضعیت - معمولا بلااستفاده، بازگرداندن خالی
            return "";
        }
    }

    /// <summary>
    /// تبدیل عدد به درصد - Number to Percentage Converter
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), out double number))
            {
                return $"{(number * 100):F1}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && str.EndsWith("%"))
            {
                if (double.TryParse(str.Replace("%",""), out double percentage))
                    return percentage/100.0;
            }
            return 0.0;
        }
    }

    /// <summary>
    /// تبدیل Boolean به Visibility - Bool to Visibility Converter
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return visibility == System.Windows.Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// تبدیل Boolean معکوس به Visibility - Inverted Bool to Visibility
    /// </summary>
    public class InvertedBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return !(visibility == System.Windows.Visibility.Visible);
            }
            return true;
        }
    }

    /// <summary>
    /// تبدیل تاریخ به فرمت قابل‌نمایش - DateTime to String Converter
    /// </summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var format = parameter?.ToString() ?? "yyyy-MM-dd HH:mm:ss";
                return dateTime.ToString(format);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value?.ToString(), out var dateTime))
            {
                return dateTime;
            }
            return DateTime.Now;
        }
    }

    /// <summary>
    /// تبدیل عدد به فرمت خوانایی - Number to Readable Format
    /// </summary>
    public class NumberFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value?.ToString(), out int number))
            {
                if (number >= 1000000)
                    return $"{(number / 1000000.0):F1}M";
                else if (number >= 1000)
                    return $"{(number / 1000.0):F1}K";
                else
                    return number.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value?.ToString(), out int number))
                return number;
            return 0;
        }
    }

    /// <summary>
    /// تبدیل Boolean به مخالف - Boolean Inverter
    /// </summary>
    public class BoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }
    }
}
