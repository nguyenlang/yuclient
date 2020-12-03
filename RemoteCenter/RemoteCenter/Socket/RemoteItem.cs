using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.WebSocket;

namespace RemoteCenter.Socket
{
    public class RemoteItem : INotifyPropertyChanged
    {
        private int _deviceName;

        public int DeviceName
        {
            get { return _deviceName; }
            set { _deviceName = value; }
        }

        private int _isConnected;

        public int Connected
        {
            get { return _isConnected; }
            set { _isConnected = value; }
        }

        private RemoteSession _remoteSession;

        public RemoteItem(RemoteSession session)
        {
            _remoteSession = session;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
