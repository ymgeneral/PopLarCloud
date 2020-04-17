using PoplarCloud.DataPacket;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace PoplarCloud
{
    public abstract class DeviceBase : IDisposable
    {

        #region "字段"
        internal Transfer transfer;
        private bool isReconnection = false;
        private NetTree netTree = new NetTree();
        private bool customDecode = false;

        internal bool CustomDecode
        {
            get { return customDecode; }
            set { customDecode = value; }
        }
        public NetTree NetTree
        {
            get { return netTree; }
            set { netTree = value; }
        }
        private AutoResetEvent registerWaite;
        private YChannel parentSocket;
        internal SocketAsyncEventArgsPool socketPool;
        private BufferManager bufferManager;
        internal int maxConnect = 500;
        private int bufferLenthg = 102400;
        private bool isInit = false;
        private string id = "";
        private bool isConnect = false;
        private int reConnectTime = 2000;
        #endregion
        public event EventHandler ParentDisconnecting;
        public event EventHandler<RaiseErrorEvent> RaiseError;
        /// <summary>
        /// 异步连接完成时发生
        /// </summary>
        public event BeginConnectCompleteCallBack BeginConnectComplete;
        #region "属性"
        /// <summary>
        /// 通道Id
        /// </summary>
        public string Id
        {
            get { return id; }
            set
            {
                id = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value;
                if (this.parentSocket != null)
                {
                    this.parentSocket.Id = id;
                }
            }
        }
        public int ReConnectTime
        {
            get { return reConnectTime; }
            set { 
                
                reConnectTime = value;
            }
        }
        public bool IsConnect
        {
            get { return isConnect; }
            private set { isConnect = value; }
        }
        public bool IsReconnection
        {
            get { return isReconnection; }
            set
			{
				isReconnection = value;
			}
        }
        public YChannel ParentSocket
        {
            get { return parentSocket; }
        }
        /// <summary>
        /// 是否初始化
        /// </summary>
        public bool IsInit
        {
            get { return isInit; }
            internal set { isInit = value; }
        }
        #endregion
        #region "抽象方法"
        public abstract void AbsInit();
        public abstract void AbsUnInit();
        public abstract void  ServiceDataPacketReceived(YChannel sender,IPacket pack);
        private IAnalysis analysis = null;
        /// <summary>
        /// 自定义解包器
        /// </summary>
        public IAnalysis Analysis
        {
            get { return analysis; }
            set {
                if(value is YDataPacketManager)
                {
                    customDecode = false;
                }
                else
                {
                    customDecode = true;
                }
                analysis = value;
                //analysis.DataPacketReceived+=analysis_DataPacketReceived;
            }
        }
   
        #endregion
        /// <summary>
        /// 初始化（必要）
        /// </summary>
        /// <param name="maxConn"></param>
        public virtual void Init(string id)
        {
            transfer = new Transfer(maxConnect);
            registerWaite = new AutoResetEvent(false);
            this.Id = id;
            YDataPacketManager.RegisterDataPacket(typeof(SplitDataPacket));
            YDataPacketManager.RegisterDataPacket(typeof(SocketRegisterPacket));
            YDataPacketManager.RegisterDataPacket(typeof(MessagePack));
            socketPool = new SocketAsyncEventArgsPool(maxConnect);
            SocketAsyncEventArgs readWriteEventArg;
            bufferManager = new BufferManager(maxConnect * bufferLenthg * 2, bufferLenthg);
            bufferManager.InitBuffer();
            for (int i = 0; i < maxConnect; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                bufferManager.SetBuffer(readWriteEventArg);
                socketPool.Push(readWriteEventArg);
            }
            netTree.CreateRoot(id);
            AbsInit();
            isInit = true;
        }
        /// <summary>
        /// 异步连接服务器
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="port">端口</param>
        /// <param name="state">标识</param>
        public void BeginConnect(string address, uint port, object state)
        {
            if (isInit == false)
            {
                OnRaiseError("未初始化，请先初始化");
                return;
            }
            if (parentSocket != null && parentSocket.IsConnect==true)
            {
                if (BeginConnectComplete != null)
                {
                    BeginConnectComplete(false, "已连接服务器，请勿重复连接!", state);
                }
                return;
            }
            transfer.ConnectParentAsync(address, port, ConnectCallBack, state);
        }
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="port">端口</param>
        /// <returns></returns>
        public YChannel Connect(string address, uint port)
        {
            if (isInit == false)
            {
				new NullReferenceException("未初始化，请先初始化");
				OnRaiseError("未初始化，请先初始化");
                return null;
            }
            Socket socket = transfer.ConnectParent(address, port);
            parentSocket = new YChannel(socket, socketPool,ChannelType.Server, "");
            parentSocket.DataPacketManager = this.Analysis;
          
            parentSocket.RaiseErrored += parentSocket_RaiseErrored;
            parentSocket.SocketDisconnecting += parentSocket_SocketDisconnecting;
            parentSocket.DataPacketReceived += parentSocket_DataPacketReceived;
            if (parentSocket != null)
            {
                parentSocket.Start();
                if (CustomDecode == false)
                {
                    Thread.Sleep(100);
                    parentSocket.Send(new SocketRegisterPacket(RegisterType.Add, netTree.Serialize()));
                    if (registerWaite.WaitOne(10000) == false)
                    {
                        parentSocket.Dispose();
						new Exception("注册失败");
                        //OnRaiseError("注册失败");
                       // return null;
                    }
                }
                IsConnect = true;
                return parentSocket;
            }
            return null;
        }

        /// <summary>
        /// 关闭当前连接
        /// </summary>
        /// <returns></returns>
        public bool CloseConnect()
        {
            if (isInit == false)
            {
               // OnRaiseError("未初始化，请先初始化");
                return false;
            }
            if (parentSocket != null)
            {
                isReconnection = false;
                parentSocket.Dispose(); 
            }
            return true;
        }
        /// <summary>
        /// 关闭当前连接并清除所占资源
        /// </summary>
        public void Dispose()
        {
            CloseConnect();
            AbsUnInit();
        }
        private  void ReConnect(string address, uint port)
        {
            Thread.Sleep(reConnectTime);
            transfer.ConnectParentAsync(address, port, ReConnectCallBack, null);
        }
        private void ReConnectCallBack(IAsyncResult e)
        {
            AsyncConnectState state = e.AsyncState as AsyncConnectState;
            if (state != null)
            {
                if (state.ConnectSocket != null)
                {
                    try
                    {
                        state.ConnectSocket.EndConnect(e);
                    }
                    catch (Exception ex)
                    {
                        if (isReconnection == true)
                        {
                            ReConnect(this.parentSocket.Address, (uint)this.parentSocket.Port);
                        }
                        return;
                    }
                }
            }
            if (e.IsCompleted)
            {
                if(parentSocket==null)
                {
                    parentSocket = new YChannel(state.ConnectSocket, socketPool,ChannelType.Server);
                    parentSocket.DataPacketManager = this.Analysis;
                    parentSocket.RaiseErrored += parentSocket_RaiseErrored;
                    parentSocket.SocketDisconnecting += parentSocket_SocketDisconnecting;
                    parentSocket.DataPacketReceived += parentSocket_DataPacketReceived;
                }
                else
                {
                    parentSocket.SetSocket(state.ConnectSocket, socketPool);
                }
                if (parentSocket != null)
                {
                    parentSocket.Start();
                    if (customDecode == false)
                    {
                        Thread.Sleep(100);
                        parentSocket.Send(new SocketRegisterPacket(RegisterType.Add, netTree.Serialize()));
                        if (registerWaite.WaitOne(10000) == false)
                        {
                            parentSocket.SocketDisconnecting -= parentSocket_SocketDisconnecting;
                            parentSocket.Dispose();
                            OnRaiseError("注册失败");
                            if (isReconnection == true)
                            {
                                ReConnect(this.parentSocket.Address, (uint)this.parentSocket.Port);
                            }
                            return;
                        }
                    }
                    IsConnect = true;
                    BeginConnectComplete(true, "", state.State);
                }
                else
                {
                    if (isReconnection == true)
                    {
                        ReConnect(this.parentSocket.Address, (uint)this.parentSocket.Port);
                    }
                }
            }
            else
            {
                if(isReconnection==true)
                {
                    ReConnect(this.parentSocket.Address, (uint)this.parentSocket.Port);
                }
            }
        }
        private void ConnectCallBack(IAsyncResult e)
        {
            BeginConnectInfo ret = e as BeginConnectInfo;
            AsyncConnectState state = e.AsyncState as AsyncConnectState;
            if (state != null)
            {
                if (state.ConnectSocket != null)
                {
                    try
                    {
                        state.ConnectSocket.EndConnect(e);
                    }
                    catch (Exception ex)
                    {
                        if (BeginConnectComplete != null)
                        {
                            BeginConnectComplete(
                                false,
                                ret != null ? ret.ErrorMessage : ex.Message,
                                state != null ? state.State : null
                                );
                        }
                        return;
                    }
                }
            }
            if (e.IsCompleted)
            {
                if (state == null)
                {
                    BeginConnectComplete(false, "连接错误", null);
                    return;
                }
                parentSocket = new YChannel(state.ConnectSocket, socketPool, ChannelType.Client);
                parentSocket.DataPacketManager = this.Analysis;
                parentSocket.RaiseErrored += parentSocket_RaiseErrored;
                parentSocket.SocketDisconnecting += parentSocket_SocketDisconnecting;
                parentSocket.DataPacketReceived += parentSocket_DataPacketReceived;
                if (parentSocket != null)
                {
                    parentSocket.Start();
                    if (CustomDecode == false)
                    {
                        Thread.Sleep(100);
                        parentSocket.Send(new SocketRegisterPacket(RegisterType.Add, netTree.Serialize()));
                        if (registerWaite.WaitOne(10000) == false)
                        {
                            parentSocket.SocketDisconnecting -= parentSocket_SocketDisconnecting;
                            parentSocket.Dispose();
                            OnRaiseError("注册失败");
                            return;
                        }
                    }
                    IsConnect = true;
                    BeginConnectComplete(true, "", state.State);
                }
                else
                {
                    BeginConnectComplete(false, "连接错误", state.State);
                }

            }
            else
            {
                if (BeginConnectComplete != null)
                {
                    BeginConnectComplete(
                        false,
                        ret != null ? ret.ErrorMessage : "连接失败",
                        state != null ? state.State : null
                        );
                }
            }

        }
        void parentSocket_SocketDisconnecting(object sender, EventArgs e)
        {
            IsConnect = false;
            if (ParentDisconnecting != null)
            {
                ParentDisconnecting(sender, e);
            }
            if (isReconnection == true)
            {
                ReConnect(this.parentSocket.Address, (uint)this.parentSocket.Port);
            }
        }

        void parentSocket_RaiseErrored(object sender, RaiseErrorEvent e)
        {
            this.OnRaiseError(e.ErrorMessage);
        }

        void parentSocket_DataPacketReceived(object sender, IPacket e)
        {
            OnDataPacketReceived(sender, e);
        }
        private void OnDataPacketReceived(object sender, IPacket e)
        {
            if (e is SocketRegisterPacket)
            {
                SocketRegisterPacket spack = e as SocketRegisterPacket;
               
                if (spack.RType == RegisterType.CallBack)
                {
                    if(!string.IsNullOrEmpty(spack.Error))
                    {
                        OnRaiseError(spack.Error);
                        return;
                    }
                    this.parentSocket.Id = spack.Data;
                    registerWaite.Set();
                }
                return;
            }
            if(e is MessagePack)
            {
                MessagePack pack = e as MessagePack;
                if(pack.TargetId.Equals(this.Id))
                {
                    ServiceDataPacketReceived((YChannel)sender, pack);
                }
                else
                {
                    this.Send(pack);
                }
                return;
            }
            ServiceDataPacketReceived((YChannel)sender, e);
        }
        internal void OnRaiseError(string text)
        {
            if (RaiseError != null)
            {
                RaiseError(this, new RaiseErrorEvent() { ErrorMessage = text });
            }
        }

        /// <summary>
        /// 发送一条数据包
        /// </summary>
        /// <param name="pack"></param>
        public virtual void Send(IPacket pack)
        {
            if (isInit == false)
            {
                OnRaiseError("未初始化，请先初始化");
                return;
            }
            if(pack is MessagePack)
            {
                MessagePack mp = pack as MessagePack;
                if (string.IsNullOrWhiteSpace(mp.FromId))
                {
                    mp.FromId = this.Id;
                }
            }
            //if(string.IsNullOrWhiteSpace(pack.FromId))
            //{
            //    pack.FromId = this.Id;
            //}
            this.parentSocket.Send(pack.Encoder());
        }
    }
}
