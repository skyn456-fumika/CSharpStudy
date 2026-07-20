using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ServerWatcher;

public class StatusToBrushConverter : IValueConverter
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
            "정상" => Brushes.Green,
            "확인 중..." => Brushes.DarkOrange,
            "HTTP 오류" => Brushes.Red,
            "연결 실패" => Brushes.Red,
            "시간 초과" => Brushes.Red,
            _ => Brushes.Gray
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