using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ServerWatcher;

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        string status = value?.ToString() ?? "";

        return status switch
        {
            "정상" => new SolidColorBrush(
                Color.FromRgb(235, 248, 238)),

            "확인 중..." => new SolidColorBrush(
                Color.FromRgb(255, 247, 225)),

            "HTTP 오류" or
            "연결 실패" or
            "시간 초과" => new SolidColorBrush(
                Color.FromRgb(255, 235, 235)),

            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}