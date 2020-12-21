using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoBuy
{
    public class StatusToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int number;
            if (int.TryParse(value.ToString(), out number))
            {
                switch (number)
                {
                    case 4:
                        return "Bought";
                    case 3:
                        return "Over price";
                    case 2:
                        return "Stock: Buy it";
                    case 1:
                        return "Checking ....";
                    default:
                        return "Out of stock";
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StatusToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int number;
            if (int.TryParse(value.ToString(), out number))
            {
                switch (number)
                {
                    case 4:
                        return new SolidColorBrush(Colors.LightBlue);
                    case 3:
                        return new SolidColorBrush(Colors.Brown);
                    case 2:
                        return new SolidColorBrush(Colors.LightGreen);
                    case 1:
                        return new SolidColorBrush(Colors.White);
                    default:
                        return new SolidColorBrush(Colors.Red);
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
