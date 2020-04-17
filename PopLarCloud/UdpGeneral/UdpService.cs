using PoplarCloud.DataPacket;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PoplarCloud.UdpGeneral
{
    public abstract class UdpService
    {
        private Socket service;
        private int maxClient = 500;
        IPAddress ipAddress;
        internal SocketAsyncEventArgsPool socketPool;
        private BufferManager bufferManager;
        private YDataPacketManager dataPacketManager;
        private int bufferLenthg = 102400;
        private bool isInit = false;
        private UpdClientTimeOut connTimeOut;
        private bool customDecode = false;


        /// <summary>
        /// 当收到完整数据包时发生（CustomDecode==fasle时有效,默认启用）
        /// </summary>
        public event DataPacketHandler DataPacketReceived;
        /// <summary>
        /// 当有异常发生时
        /// </summary>
        public event EventHandler<RaiseErrorEvent> RaiseErrored;
        /// <summary>
        /// 收到数据是发生（CustomDecode==true时有效）
        /// </summary>
        public event DataAvailableHandler DataAvailable;
        protected abstract void AbsInit();

        public bool CustomDecode
        {
            get { return customDecode; }
            set { customDecode = value; }
        }
        public void Init(int maxClient)
        {
            connTimeOut = new UpdClientTimeOut();
            connTimeOut.TimeOut = 1800;
            connTimeOut.Start();
            dataPacketManager = new YDataPacketManager();
            YDataPacketManager.RegisterDataPacket(typeof(SocketRegisterPacket));
            dataPacketManager.DataPacketReceived+=dataPacketManager_DataPacketReceived;
            this.maxClient=maxClient+1;
            socketPool = new SocketAsyncEventArgsPool(maxClient);
            SocketAsyncEventArgs readWriteEventArg;
            bufferManager = new BufferManager(maxClient * bufferLenthg, bufferLenthg);
            bufferManager.InitBuffer();
            for (int i = 0; i < maxClient; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += readWriteEventArg_Completed;
                bufferManager.SetBuffer(readWriteEventArg);
                socketPool.Push(readWriteEventArg);
            }
            AbsInit();
            isInit = true;
        }

        void readWriteEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch(e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    socketPool.Push(e);
                    break;
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            IPEndPoint ep = e.RemoteEndPoint as IPEndPoint;
            if (e.SocketError == SocketError.Success)
            {
                byte[] newData = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, newData, 0, e.BytesTransferred);
                if (CustomDecode == false)
                {
                    try
                    {
                        dataPacketManager.ReceivedData(newData);
                    }
                    catch (Exception ex)
                    {
                        OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message });
                    }
                }
                else
                {
                    OnDataAvailable(newData);
                }
            }
            BeginReceive(e);
        }
        private void BeginReceive(SocketAsyncEventArgs e)
        {
            if(this.service==null)
            {
                return;
            }
            if(!this.service.ReceiveFromAsync(e))
            {
                ProcessReceive(e);
            }
        }
        public void Send(DataPacketBase e ,IPEndPoint ipe)
        {
            service.SendToAsync(socketPool.Pop());
        }
        public bool Start(uint listenerPort,uint dataPort)
        {
            if(!isInit)
            {
                throw new Exception("未初始化，请先调用Init");
            }
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, (int)dataPort);
            service = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            service.Bind(ipep);
            BeginReceive(socketPool.Pop());
            return true;
        }
        public bool Stop()
        {
            service.Close();
            service=null;
            return true;
        }
        private void OnRaiseErrored(RaiseErrorEvent raiseErrorEvent)
        {
            if (RaiseErrored != null)
            {
                RaiseErrored(this, raiseErrorEvent);
            }
        }
        void dataPacketManager_DataPacketReceived(object sender, IPacket e)
        {
            if (DataPacketReceived != null)
            {
                DataPacketReceived(this, e);
            }
        }
        private void OnDataAvailable(byte[] newData)
        {
            if (DataAvailable != null)
            {
                DataAvailable(this, newData);
            }
        }
    }
}
