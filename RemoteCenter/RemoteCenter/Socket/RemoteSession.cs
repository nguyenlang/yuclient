using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Protocol;

namespace RemoteCenter.Socket
{
    public class RemoteSession : WebSocketSession<RemoteSession>
    {
        public int deviceNo;
        protected override void OnSessionStarted()
        {
            base.OnSessionStarted();
        }

        protected override void HandleException(Exception e)
        {
            base.HandleException(e);
        }

        protected override void HandleUnknownRequest(IWebSocketFragment requestInfo)
        {
            base.HandleUnknownRequest(requestInfo);
        }

    }
}
