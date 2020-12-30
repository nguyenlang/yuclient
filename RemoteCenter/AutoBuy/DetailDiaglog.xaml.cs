using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoBuy
{
    /// <summary>
    /// Interaction logic for DetailDiaglog.xaml
    /// </summary>
    public partial class DetailDiaglog : Window
    {
        private BuyItemModel _originBuyItem;
        private object _lockobj;
        public BuyItemModel ModifyBuyItem { set; get; } = new BuyItemModel();

        public DetailDiaglog()
        {
            InitializeComponent();
            this.DataContext = ModifyBuyItem;
        }

        public void Initialize(BuyItemModel item,object lockobj)
        {
            _originBuyItem = item;
            ModifyBuyItem.Name = item.Name;
            ModifyBuyItem.Asin = item.Asin;
            ModifyBuyItem.MaxPrice = item.MaxPrice;
            ModifyBuyItem.BuyLimit = item.BuyLimit;
            ModifyBuyItem.Status = item.Status;

            _lockobj = lockobj;
        }


        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            lock (_lockobj)
            {
                _originBuyItem.Name = ModifyBuyItem.Name;
                _originBuyItem.Asin = ModifyBuyItem.Asin;
                _originBuyItem.MaxPrice = ModifyBuyItem.MaxPrice;
                _originBuyItem.BuyLimit = ModifyBuyItem.BuyLimit;
                _originBuyItem.Status = ModifyBuyItem.Status;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
