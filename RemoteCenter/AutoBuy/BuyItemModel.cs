using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBuy
{
    public class BuyItemModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Asin { get; set; }

        private double _maxPrice = 0;

        public double MaxPrice
        {
            get { return _maxPrice; }
            set
            {
                if (_maxPrice != value)
                {
                    _maxPrice = value;
                    OnPropertyChanged("MaxPrice");
                }
            }
        }

        private int _status = 0; //0: out of stock, 1: checking, 2: stock ->buy, 3: Over price, 4: Bought
        [JsonIgnore]
        public int Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        private double _price;
        [JsonIgnore]
        public double Price
        {
            get { return _price; }
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged("Price");
                }
            }
        }

        private int _buyLimit  = 1;

        public int BuyLimit
        {
            get { return _buyLimit; }       
            set { _buyLimit = value; }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return Name + " - " + Asin + " : " + MaxPrice;
        }
    }
}
