using PoplarCloud.DataPacket;
using PoplarCloud.EventsAndEnum;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PoplarCloud
{
    /// <summary>
    /// 网络连接
    /// </summary>
    internal sealed class Transfer
    {
        #region "事件"
        /// <summary>
        /// 当有客户端连接时发生
        /// </summary>
        public event SocketConnectedHandler Connected;
        /// <summary>
        /// 当有异常时发生
        /// </summary>
        public event EventHandler<RaiseErrorEvent> RaiseErrored;
        #endregion
        private TcpListener tcpListener;
        private int maxConnect = 3000;
        private bool isWebServer;
        public Transfer(int maxConn)
        {
            this.maxConnect = maxConn;
            YDataPacketManager.RegisterDataPacket(typeof(SplitDataPacket));
            Init();
        }
        private void Init()
        {


        }
        public bool Stop()
        {
            if (tcpListener == null)
            {
                OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = "服务未开启！" });
                return false;
            }
           bool ret= tcpListener.Stop();
            tcpListener = null;
            return ret;
        }
        public bool CreateService(uint port,bool isWebService=false)
        {
            isWebServer = isWebService;
            if(tcpListener!=null)
            {
                OnRaiseErrored(new RaiseErrorEvent() {  ErrorMessage ="服务已经开启！" });
                return false;
            }
            if (port < 1000 || port > 65535)
            {
                OnRaiseErrored(new RaiseErrorEvent() {  ErrorMessage = "无效的端口,端口请设置在1000-65535之间" });
                return false;
            }
            tcpListener=new TcpListener((int)port);
            tcpListener.Connected += tcpListener_Connected;
            try
            {
                tcpListener.Init((uint)maxConnect);
                tcpListener.Start();
                return true;
            }
            catch(Exception ex)
            {
                OnRaiseErrored(new RaiseErrorEvent() {  ErrorMessage = ex.Message });
                tcpListener.Stop();
                tcpListener = null;
                return false;
            }
        }
        public void ConnectParentAsync(string netAddress, uint port,AsyncCallback callBack,object state)
        {
            if (port < 1000 || port > 65535)
            {
                callBack(new BeginConnectInfo(false, state, "无效的端口,端口请设置在1000-65535之间"));
                return ;
            }
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if(socket==null)
                {
                    return;
                }
                IPEndPoint iep = GetIPEndPoint(netAddress, port);
                if(iep==null)
                {
                    return;
                }
                socket.BeginConnect(iep, callBack, new AsyncConnectState(socket,state));
            }
            catch (Exception ex)
            {
                string error = string.Format("{0} 连接地址：{1},端口：{2}", ex.Message, netAddress, port);
                //callBack()
                callBack(new BeginConnectInfo(false, state,  error));
            }
        }
        private IPEndPoint GetIPEndPoint(string netAddress, uint port)
        {
           
            IPAddress iPAddress = IpValidation(netAddress);
            if (iPAddress == null)
            {
                return null;
            }
            IPEndPoint iep = new IPEndPoint(iPAddress, (int)port);
            return iep;
        }
        private IPAddress IpValidation(string netaddress)
        {
            IPAddress iPAddress = null;
            if (General.IsIPv4Address(netaddress))
            {
                iPAddress = IPAddress.Parse(netaddress);
            }
            else
            {
                try
                {
                    iPAddress = General.ResolveDns(netaddress);
                }
                catch (Exception ex)
                {
                    OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message });
                }
                if (iPAddress == null)
                {
                    OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = "无效的地址，请确保地址是有效的IP或域名" });
                }
            }
            return iPAddress;
        }
        public Socket ConnectParent(string netAddress, uint port)
        {
            if (port < 1000 || port > 65535)
            {
                OnRaiseErrored(new RaiseErrorEvent() {  ErrorMessage = "无效的端口,端口请设置在1000-65535之间" });
                return null;
            }
            try
            {
                
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (socket == null)
                {
                    return null;
                }
                IPEndPoint iep = GetIPEndPoint(netAddress, port);
                if (iep == null)
                {
                    return null;
                }
                socket.Connect(iep);
                return socket;
            }
            catch(Exception ex)
            {
                
                string error = string.Format("{0}连接地址：{1},端口：{2}", ex.Message, netAddress, port);
                return null;
            }
         
        }

        private  void tcpListener_Connected(object sender, System.Net.Sockets.Socket e)
        {
            OnConnected(sender, e);
        }
        private void OnConnected(object sender,Socket e)
        {
            if (Connected != null)
            {
                Connected(sender, e);
            }
        }

        private void OnRaiseErrored(RaiseErrorEvent e)
        {
            if (RaiseErrored != null)
            {
                RaiseErrored(this, e);
            }
        }
    }
}
