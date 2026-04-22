using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskGrowthApp
{
    public class EmergencyForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int priority)
                return Brushes.White;

            return priority switch
            {
                0 => Brushes.Green,   // 低
                1 => Brushes.Yellow,  // 中
                2 => Brushes.Red,     // 高
                3 => Brushes.White,   // 緊急（背景が赤なので白）
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}