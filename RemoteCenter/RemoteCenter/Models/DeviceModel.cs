using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteCenter.Models
{
    public class DeviceModel : INotifyPropertyChanged
    {
        private Boolean _isSelect;

        public Boolean IsSelect
        {
            get { return _isSelect; }
            set
            {
                if (_isSelect != value)
                {
                    _isSelect = value;
                    OnPropertyChanged("IsSelect");
                }
            }
        }

        public string PortNo { get; set; }
        public string Id { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
