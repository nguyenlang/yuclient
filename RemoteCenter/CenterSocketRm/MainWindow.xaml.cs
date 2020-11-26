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
using SuperSocket.WebSocket;

namespace CenterSocketRm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebSocketServer appServer = new WebSocketServer();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AppSever_NewMessageReceived(WebSocketSession session, string value)
        {
            //receive message from session
            Console.WriteLine("Message" + value);
            session.Send("Server: receive" + value);
        }

        private void AppSever_NewSessionConnected(WebSocketSession session)
        {
            //Add websocket session
            Console.WriteLine("Connected session: " + session.SessionID);
        }

        private void StartServer(object sender, RoutedEventArgs e)
        {
            //setup the appserver
            if (!appServer.Setup(9999))
            {
                Console.WriteLine("Fail to setup");
                return;
            }

            appServer.NewSessionConnected += AppSever_NewSessionConnected;
            appServer.NewMessageReceived += AppSever_NewMessageReceived;
            appServer.SessionClosed += AppServer_SessionClosed;

            Console.WriteLine("Start");
            if (!appServer.Start())
            {
                Console.WriteLine("Failed to start!");
                return;
            }

            Console.WriteLine("The server started successfully, press key 'q' to stop it!");

        }

        private void AppServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            
        }

        private void StopServer(object sender, RoutedEventArgs e)
        {
            appServer.Stop();
            //appServer.Dispose();
            Console.WriteLine();
            Console.WriteLine("The server was stopped!");

        }
    }
}
