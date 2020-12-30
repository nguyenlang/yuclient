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
            BuyStatus status = (BuyStatus) value;

            switch (status)
            {
                case BuyStatus.BOUGHT:
                    return "Bought";
                case BuyStatus.OVER_PRICE:
                    return "Over price";
                case BuyStatus.BUYING:
                    return "Buying...";
                case BuyStatus.CHECKING:
                    return "Checking...";
                case BuyStatus.OUT_OF_STOCK:
                    return "Out of stock";
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
            BuyStatus status = (BuyStatus)value;

            switch (status)
            {
                case BuyStatus.BOUGHT:
                    return new SolidColorBrush(Colors.LightBlue);
                case BuyStatus.OVER_PRICE:
                    return new SolidColorBrush(Colors.Brown);
                case BuyStatus.BUYING:
                    return new SolidColorBrush(Colors.LightGreen);
                case BuyStatus.CHECKING:
                    return new SolidColorBrush(Colors.White);
                case BuyStatus.OUT_OF_STOCK:
                    return new SolidColorBrush(Colors.Red);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
