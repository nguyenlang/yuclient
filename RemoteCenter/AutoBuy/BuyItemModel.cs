using Newtonsoft.Json;
using System.ComponentModel;
using System.Threading;

namespace AutoBuy
{
    public enum BuyStatus{
        OUT_OF_STOCK,
        CHECKING,
        BUYING,
        BOUGHT,
        OVER_PRICE
    }

    public class BuyItemModel : INotifyPropertyChanged
    {

        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                if (!_name.Equals(value))
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        private string _asin = string.Empty;

        public string Asin
         {
            get { return _asin; }
            set
            {
                if (!_asin.Equals(value))
                {
                    _asin = value;
                    OnPropertyChanged("Asin");
                }
            }
        }

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

        private BuyStatus _status = BuyStatus.OUT_OF_STOCK; //0: out of stock, 1: checking, 2: stock ->buy, 3: Over price, 4: Bought
        [JsonIgnore]
        public BuyStatus Status
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
            set {
                if (_buyLimit != value)
                {
                    _buyLimit = value;
                    OnPropertyChanged("BuyLimit");
                }
            }
        }

        private int _numberBought = 0;
        [JsonIgnore]
        public int NumberBought
        {
            get { return _numberBought; }
            set
            {
                if (_numberBought != value)
                {
                    _numberBought = value;
                    OnPropertyChanged("NumberBought");
                }
            }
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

        [JsonIgnore]
        public BuyService BuyService = null;

        [JsonIgnore]
        public int BuyServiceIndex = 0;
        [JsonIgnore]
        public CancellationTokenSource CancelSource = null;
    }
}
