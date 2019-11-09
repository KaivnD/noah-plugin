using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using WebSocketSharp;

namespace NoahPlugin
{

    public class WsObject
    {
        private WebSocket webSocket;

        public int status;

        public string message;

        private string initMessage;

        public event EventHandler changed;

        public WsObject init(string address, string initMessage)
        {
            webSocket = new WebSocket(address);
            this.initMessage = initMessage;
            webSocket.WaitTime = new TimeSpan(0, 0, 2);
            connect();
            return this;
        }

        protected virtual void onChanged()
        {
            this.changed?.Invoke(this, EventArgs.Empty);
        }

        private WsObject connect()
        {
            webSocket.OnOpen += onOpen;
            webSocket.OnMessage += onMessage;
            webSocket.OnError += onError;
            webSocket.OnClose += onClose;
            webSocket.Connect();
            return this;
        }

        public WsObject disconnect()
        {
            webSocket.OnOpen -= onOpen;
            webSocket.OnMessage -= onMessage;
            webSocket.OnError -= onError;
            webSocket.OnClose -= onClose;
            webSocket.Close();
            return this;
        }

        private void onOpen(object sender, EventArgs e)
        {
            send(initMessage);
            status = WsObjectStatus.OPEN;
            onChanged();
        }

        private void onError(object sender, ErrorEventArgs e)
        {
            status = WsObjectStatus.CLOSE;
            webSocket = null;
            onChanged();
        }

        private void onMessage(object sender, MessageEventArgs e)
        {
            status = WsObjectStatus.MESSAGE;
            if (!e.IsPing)
            {
                message = e.Data;
            }
            onChanged();
        }

        private void onClose(object sender, CloseEventArgs e)
        {
            status = WsObjectStatus.CLOSE;
            webSocket = null;
            onChanged();
        }

        public void send(string msg)
        {
            if (webSocket != null && webSocket.IsAlive)
            {
                webSocket.Send(msg);
            }
        }
    }

    internal class WsObjectStatus
    {
        public static int ERROR = 0;

        public static int OPEN = 1;

        public static int MESSAGE = 2;

        public static int CLOSE = 3;

        public static string GetStatusName(int status)
        {
            switch (status)
            {
                case 0:
                    return "Error";
                case 1:
                    return "Open";
                case 2:
                    return "Message";
                case 3:
                    return "Close";
                default:
                    return "UNKOWN";
            }
        }
    }
}
