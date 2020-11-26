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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.WebSockets;
using System.Threading;

namespace SocketClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientWebSocket socket;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectServe_Click(object sender, RoutedEventArgs e)
        {
            socket = new ClientWebSocket();
            Console.WriteLine("Connecting");
            socket.ConnectAsync(new Uri("ws://localhost:9999"), CancellationToken.None).Wait();

            Console.WriteLine("Sending");
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }




    }
}
