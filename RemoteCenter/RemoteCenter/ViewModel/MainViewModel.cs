using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NLog;
using RemoteCenter.Models;
using RemoteCenter.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace RemoteCenter.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        #region properties
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private string _videoUrl;

        public string VideoUrl
        {
            get { return _videoUrl; }
            set { _videoUrl = value; }
        }

        private Boolean _isSelectAll;

        public Boolean IsSelectAll
        {
            get { return _isSelectAll; }
            set { 
                if(_isSelectAll != value)
                {
                    _isSelectAll = value;
                    UpdateListDeviceSelected(_isSelectAll);
                }
            }
        }

        public ObservableCollection<DeviceModel> DevicesList { get; set; } = new ObservableCollection<DeviceModel>();

        #endregion

        #region Command
        private RelayCommand _getListDevice;

        public RelayCommand GetListDevice
        {
            get {
                return _getListDevice ?? (_getListDevice = new RelayCommand(() =>
              {
                  //get device list
                  var argument = "devices -l";
                  string output = CommandLineExecuter.Execute(CommonConfig.AdbFilePath, argument);
                  string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                  // Ignore firs line
                  //02157df21006390e device product: zerofltemtr model:SM_G920T1 device:zerofltemtr transport_id:1
                  DevicesList.Clear();
                  for(int i = 1; i < lines.Length; i++)
                  {
                      //TODO: Improve later
                      string[] properties = lines[i].Split(new[] {':',' '}, StringSplitOptions.RemoveEmptyEntries);
                      int len = properties.Length;

                      var deviceInfo = new DeviceModel
                      {
                          Id = properties[0],
                          Model = len < 6 ? string.Empty : properties[5] ,
                          Name = len < 4 ? string.Empty : properties[3],
                          PortNo = len < 10 ? string.Empty : properties[9]
                      };

                      DevicesList.Add(deviceInfo);
                  }

              })); }
        }

        private RelayCommand _runVideo;

        public RelayCommand RunVideo
        {
            get {
                return _runVideo ?? (_runVideo = new RelayCommand(() =>
                {
                    var selectedDevices = DevicesList.Where(d => d.IsSelect == true);

                    if(selectedDevices.Count() <= 0)
                    {
                        _logger.Info("Device list is empty");
                        return;
                    }

                    if (string.IsNullOrEmpty(VideoUrl))
                        return;

                    foreach(var device in selectedDevices)
                    {

                        //Stop
                        var stopArgument = $"-s {device.Id} {CommonConfig.StopYoutube}";
                        CommandLineExecuter.Execute(CommonConfig.AdbFilePath, stopArgument);
                        Thread.Sleep(100);

                        var argument = $"-s {device.Id} {CommonConfig.PrefixCmd} \"{VideoUrl}\"";
                        CommandLineExecuter.Execute(CommonConfig.AdbFilePath, argument);

                    }


                }));}

        }

        private RelayCommand _stop;

        public RelayCommand Stop
        {
            get
            {
                return _stop ?? (_stop = new RelayCommand(() =>
                {
                    var selectedDevices = DevicesList.Where(d => d.IsSelect == true);
                    if (selectedDevices.Count() <= 0)
                    {
                        _logger.Info("Device list is empty");
                        return;
                    }

                    foreach (var device in selectedDevices)
                    {

                        //Stop
                        var stopArgument = $"-s {device.Id} {CommonConfig.StopYoutube}";
                        CommandLineExecuter.Execute(CommonConfig.AdbFilePath, stopArgument);
                    }
                }));
            }

        }
        #endregion

        private void UpdateListDeviceSelected(bool isSelect)
        {
            foreach(var device in DevicesList)
            {
                device.IsSelect = isSelect;
            }
        }


        public MainViewModel()
        {


        }
    }
}