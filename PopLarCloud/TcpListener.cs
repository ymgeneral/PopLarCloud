using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using PoplarCloud.EventsAndEnum;
namespace PoplarCloud
{

    internal class TcpListener:IDisposable
    {

       
        /// <summary>
        /// 当有客户端连接时发生
        /// </summary>
        public event SockeConnectedHandler Connected;
        private Socket socket;
        private int port;


        public ListenerState State { get; private set; }
        public TcpListener(int port)
        {
            State = ListenerState.Idle;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.port = port;
        }

        public void Init(uint maxConnetion = 500)
        {

            if (State != ListenerState.Idle)
                return;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, port);
            //if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            //{
            //    // 配置监听socket为 dual-mode (IPv4 & IPv6)   
            //    // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,  
            //    _serverSock.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            //    _serverSock.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            //}
            //else
            //{
            //    _serverSock.Bind(localEndPoint);
            //}  
            try
            {
                socket.Bind(iep);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            socket.Listen((int)maxConnetion);
            State = ListenerState.Loaded;
           
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        /// <returns></returns>
        public void Start(SocketAsyncEventArgs acceptEventArgs=null)
        {
            if (State != ListenerState.Loaded)
            {
                Init();
            }

            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(e_Completed);
            }
            else
            {
                acceptEventArgs.AcceptSocket = null; //释放上次绑定的Socket，等待下一个Socket连接  
            }
            bool willRaiseEvent = socket.AcceptAsync(acceptEventArgs);
            if (!willRaiseEvent)
            {
                Start();
            }  
            State = ListenerState.Runing;
           
        }

        void e_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                Socket socket = e.AcceptSocket;
                OnConnected(socket, false);
                Start(e);
            }
            catch { }
        }
        public bool Stop()
        {
            if (State != ListenerState.Runing)
            {
                return true;
            }
            socket.Close();
            State = ListenerState.Idle;
            return true;
        }
        protected void OnConnected(Socket socket,bool isweb=false)
        {
            if(Connected!=null)
            {
                Connected(isweb, socket);
            }
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}
