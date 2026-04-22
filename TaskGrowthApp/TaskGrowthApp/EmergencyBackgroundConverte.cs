using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskGrowthApp
{
    public class EmergencyBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int priority && priority == 3) // 緊急
                return Brushes.DarkRed;
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
