//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Net;
//using System.Net.Sockets;
//using PoplarCloud.EventsAndEnum;
//using System.Threading;
//using System.IO;
//using PoplarCloud.DataPacket;
//using System.ServiceModel.Channels;
//namespace PoplarCloud
//{
//    public class SocketManager :EventArgs, IDisposable
//    {
//        #region "字段"
//        private bool isDisconnect = false;
//        private Socket socket;//当前连接
//        private byte[] receiveBuffer;//数据缓存
//        private Thread sendingThread;//发送消息线程
//        private DataPacketManager dataPacketManager;//数据包管理器
//        private SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
//        /// <summary>
//        /// 发送数据包信号
//        /// </summary>
//        private ManualResetEvent sendManual;
//        /// <summary>
//        /// 关闭发生数据包
//        /// </summary>
       
//        private ManualResetEvent closeSendManual;
//        private Dictionary<string,List<PoplarCloud.DataPacket.SplitDataPacket>> splitPackDic = new Dictionary<string,List<DataPacket.SplitDataPacket>>();
//        private Queue<byte[]> queue = new Queue<byte[]>();
//        private Queue<byte[]> splitePackQueue = new Queue<byte[]>();
//        private Thread clientHeartThead;
//        private Thread serviceHeartThead;
//        private bool isWebSocket = false;
//        #endregion

//        #region "属性"
//        internal bool IsWebSocket
//        {
//            get { return isWebSocket; }
//            set { isWebSocket = value; }
//        }
//        private IPAddress iPAddress;
//        /// <summary>
//        /// 服务器网络协议地址
//        /// </summary>
//        public IPAddress IPAddress
//        {
//            get
//            { 
//                return iPAddress;
//            }
//            private  set { iPAddress = value; }
//        }
//        private string netAddress;

//        public string NetAddress
//        {
//            get 
//            {
//                if(string.IsNullOrEmpty(netAddress))
//                {
//                    return IPAddress.ToString();
//                }
//                return netAddress; 
//            }
//            private set 
//            {
//                netAddress = value; 
//            }
//        }
//        private int port;

//        public int Port
//        {
//            get { return port; }
//            private set { port = value; }
//        }
//        private bool isOpenSplitePacket = true;
//        /// <summary>
//        /// 是否开始自动分包
//        /// 当IsDecode为true是有效，否则此设置无效
//        /// </summary>
//        public bool IsOpenSplitePacket
//        {
//            get { return isOpenSplitePacket; }
//            set { isOpenSplitePacket = value; }
//        }
//        private int bufferLength = 102400;
//        /// <summary>
//        /// 接受数据缓冲区大小默认100KB,最小1KB
//        /// 此缓冲去大小也代表分包条件，如IsOpenSplitePacket是true时数据包大于BufferLength将自动分包
//        /// </summary>
//        public int BufferLength
//        {
//            get { return bufferLength; }
//            set {
//                if (value < 1024)
//                {
//                    bufferLength = 1024;
//                }
//                else
//                    bufferLength = value;
//            }
//        }
//        private bool isDecode = true;
//        /// <summary>
//        /// 是否启用编码数据包（默认编码）
//        /// </summary>
//        public bool IsDecode
//        {
//            get { return isDecode; }
//            set { isDecode = value; }
//        }
//        private bool isRuning = false;
//        /// <summary>
//        /// 是否运行
//        /// </summary>
//        public bool IsRuning
//        {
//            get { return isRuning; }
//            private  set { isRuning = value; }
//        }
//        private bool isBackground = false;
//        /// <summary>
//        /// 是否开启后台线程
//        /// </summary>
//        public bool IsBackground
//        {
//            get
//            {
//                return isBackground;
//            }
//            set
//            {
//                isBackground = false;
//                if (sendingThread != null)
//                {
//                    sendingThread.IsBackground = value;
//                }
//                if(clientHeartThead!=null)
//                {
//                    clientHeartThead.IsBackground = value;
//                }
//                if(serviceHeartThead!=null)
//                {
//                    serviceHeartThead.IsBackground = true;
//                }
//            }
//        }
//        private int timeOutSeconds = 8;
//        /// <summary>
//        /// 心跳间隔时间（单位秒,最小5秒）
//        /// 心跳超时为此时间4倍
//        /// </summary>
//        public int TimeOutSeconds
//        {
//            get { return timeOutSeconds; }
//            set 
//            {
//                if (value < 5)
//                {
//                    timeOutSeconds = 5;
//                }
//                else
//                {
//                    timeOutSeconds = value;
//                }
//            }
//        }
//        private bool serviceHeartBeat = false;
//        /// <summary>
//        /// 服务器心跳开关
//        /// </summary>
//        public bool ServiceHeartBeat
//        {
//            get { return serviceHeartBeat; }
//            set
//            {
//                if(IsDecode==false)
//                {
//                    serviceHeartBeat = false;
//                    return;
//                }
//                if (serviceHeartBeat == false)
//                {
//                    serviceHeartBeat = value;
//                    serviceHeartThead = new Thread(new ThreadStart(ServiceHeart));
//                    serviceHeartThead.Start();
//                }
//                serviceHeartBeat = value;
//            }
//        }
//        private bool clientHeartBeat = false;
//        /// <summary>
//        /// 客户端心跳开关
//        /// </summary>
//        public bool ClientHeartBeat
//        {
//            get { return clientHeartBeat; }
//            set
//            {
//                if (IsDecode == false)
//                {
//                    clientHeartBeat = false;
//                    return;
//                }
//                if (clientHeartBeat == false)
//                {
//                    clientHeartBeat = value;
//                    clientHeartThead = new Thread(new ThreadStart(HeartBeat));
//                    clientHeartThead.Start();
//                }
//                serviceHeartBeat = value;
//            }
//        }
//        private string guid;
//        public string Guid
//        {
//            get { return guid; }
//            set { guid = value; }
//        }
//        #endregion

//        #region "事件"
//        /// <summary>
//        /// 当连接断开时发生
//        /// </summary>
//        public event EventHandler SocketDisconnecting;
//        /// <summary>
//        /// 收到数据是发生
//        /// </summary>
//        public event DataAvailableHandler DataAvailable;
//        /// <summary>
//        /// 当收到完整数据包时发生
//        /// </summary>
//        public event DataPacketHandler DataPacketReceived;
//        /// <summary>
//        /// 当有异常发生时
//        /// </summary>
//        public event EventHandler<RaiseErrorEvent> RaiseErrored;
//        /// <summary>
//        /// 异步连接回调
//        /// </summary>
//        private AsyncCallback beginConnectCompleteCallBack;
//        private object beginSender;
//        #endregion
//        #region "公共方法"
//        public void Init()
//        {

//        }
//        public SocketManager(Socket socket,bool iswebSocket=false)
//        {
//            if (this.socket != null)
//            {
//                return;
//            }
//            this.IsWebSocket = iswebSocket;
//            IsRuning = true;
//            this.socket = socket;
//            receiveBuffer = new byte[bufferLength];
//            IPAddress = ((IPEndPoint)socket.RemoteEndPoint).Address;
//            Port  = ((IPEndPoint)socket.RemoteEndPoint).Port;
//            dataPacketManager = new DataPacketManager();
//            dataPacketManager.DataPacketReceived+=dataPacketManager_DataPacketReceived;
//            readWriteEventArg.SetBuffer(receiveBuffer,0, bufferLength);

//            readWriteEventArg.Completed += readWriteEventArg_Completed;
//        }
//        internal SocketManager(string netAddess, uint port, bool iswebSocket = false)
//        {
//            if (socket != null)
//            {
//                return;
//            }
//            this.IsWebSocket = iswebSocket;
//            IPAddress = System.Net.IPAddress.Loopback;
//            if (General.IsIPv4Address(netAddess))
//            {
//                IPAddress = IPAddress.Parse(netAddess);
//            }
//            else
//            {
//                try
//                {
//                    IPAddress = General.ResolveDns(netAddess);
//                }
//                catch (Exception ex)
//                {
//                    throw ex;
//                }
//                if (iPAddress == null)
//                {
//                    throw new Exception("无效的地址，请确保地址是有效的IP或域名");
//                }
//            }
//            this.NetAddress = netAddess;
//            this.Port = (int)port;
//            receiveBuffer = new byte[bufferLength];
//            readWriteEventArg.SetBuffer(receiveBuffer,0, bufferLength);
//            readWriteEventArg.Completed += readWriteEventArg_Completed;
//        }
//        void readWriteEventArg_Completed(object sender, SocketAsyncEventArgs e)
//        {
//            switch (e.LastOperation)
//            {
//                case SocketAsyncOperation.Receive:
//                    ProcessReceive(e);
//                    break;
//                case SocketAsyncOperation.Send:
//                    ProcessSend(e);
//                    break;
//                default:
//                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
//            }   
//        }

//        private void ProcessSend(SocketAsyncEventArgs e)
//        {

//        }

//        private void ProcessReceive(SocketAsyncEventArgs e)
//        {
//            if (e.BytesTransferred>0 && e.SocketError== SocketError.Success)
//            {
//                byte[] newData = new byte[e.BytesTransferred];
//                Array.Copy(e.Buffer, 0, newData, 0, e.BytesTransferred);
//                if (IsDecode == true)
//                {
//                    try
//                    {
//                        dataPacketManager.Write(newData);
//                    }
//                    catch (Exception ex)
//                    {
//                        OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message});
//                    }
//                }
//                else
//                {
//                    if (newData.Length > 0)
//                    {
//                        if (isWebSocket == true)
//                        {
//                            WriteData(newData);
//                        }
//                        else
//                        {
//                            OnDataAvailable(newData);
//                        }
//                    }
//                }
               
//            }
//            else
//            {
//                Disconnect();
//            }
//        }

//        public void Start()
//        {
//            if(socket!=null)
//            {
//                socket.SendAsync(readWriteEventArg);
//                socket.ReceiveAsync(readWriteEventArg);
//            }
//        }
//        private static object lockObject = new object();
//        public void Send(DataPacketBase packet)
//        {
//            if (packet == null)
//            {
//                return;
//            }
//             byte[] bytes = packet.Encoder();
//             Send(bytes);
//        }
//        public bool Disconnect()
//        {
//           return  Disconnect("请求退出成功！", DisconnectType.Null);
//        }
//        public void Send(byte[] bytes)
//        {
//            lock (lockObject)
//            {
//                readWriteEventArg.SetBuffer(bytes, 0, bytes.Length);
//            }
//          //  AddQueue(bytes);
//        }
//        public void Dispose()
//        {
//            this.Disconnect("请求退出成功！", DisconnectType.Null);
//        }
//        #endregion
//        private bool Disconnect(string message,DisconnectType dtype)
//        {
//            if (isDisconnect)
//            {
//                return true;
//            }
//            isDisconnect = true;
//            lastPackTime = DateTime.MinValue;
//            clientHeartBeat = false;
//            serviceHeartBeat = false;
//            if (clientHeartThead != null)
//            {
//                clientHeartThead.Abort();
//            }
//            queue.Clear();
//            splitePackQueue.Clear();
//            lock (this)
//            {
//                if (IsRuning == false)
//                {
//                    return true;
//                }
//                if(closeSendManual!=null)
//                closeSendManual.Set();
//                if(sendManual!=null)
//                sendManual.Set();
//                if (sendingThread != null && sendingThread.ThreadState == ThreadState.Running)
//                   sendingThread.Join();
//                try { socket.Shutdown(SocketShutdown.Receive); }
//                catch { }
//                this.socket.Close();
//                this.readWriteEventArg.Dispose();
//                OnSocketDisconnecting(message, dtype);
//                IsRuning = false;
//            }
//            isDisconnect = false;
//            return true;
//        }
//        /// <summary>
//        /// 直接发byte数组，分包无效
//        /// </summary>
//        /// <param name="bytes"></param>
      
     

//        private void SendSplit(byte[] buffer)
//        {
            
//            long dataLength = buffer.LongLength;
//            int buffLength = BufferLength - 120;
//            int packCount = (int)(dataLength / buffLength + (dataLength % buffLength == 0 ? 0 : 1));
//            string packIid=System.Guid.NewGuid().ToString();
//            int copyLenth = buffLength;
//            for (int i = 0; i < packCount; i++)
//            {
//                long nowLength = dataLength - buffLength * i;
//                if (nowLength < BufferLength)
//                {
//                    copyLenth = (int)nowLength;
//                }
//                SplitDataPacket splitPack = new SplitDataPacket();
//                splitPack.SplitPackId = packIid;
//                splitPack.PackIndex = i + 1;
//                splitPack.PackCount = packCount;
//                splitPack.PackLength = buffer.Length;
//                splitPack.PackLength = dataLength;
//                byte[] newData = new byte[copyLenth];
//                Buffer.BlockCopy(buffer, i * buffLength, newData, 0, copyLenth);
//                splitPack.Data = newData;
//                AddSplitQueue(splitPack.Encoder());
//            }
//            sendManual.Set();
//        }
//        private void ServiceHeart()
//        {
//            while (serviceHeartBeat)
//            {
//                if (lastPackTime == DateTime.MinValue)
//                {
//                    break;
//                }
//                if (lastPackTime < DateTime.Now.AddSeconds(-(timeOutSeconds * 4)))
//                {
//                    this.Disconnect("超时", DisconnectType.TimeOut);
//                    break;
//                }
//                Thread.Sleep(timeOutSeconds * 1000);
//            }
//        }
//        private void HeartBeat()
//        {
//            byte[] buffer = new PoplarCloud.DataPacket.HeartPack().Encoder();
//            while (clientHeartBeat)
//            {
//                Thread.Sleep(timeOutSeconds * 800);
//#if DEBUG
//                Console.WriteLine(DateTime.Now.ToString() + "发送了心跳包");
//#endif
//                if (queue.Contains(buffer) == false)
//                {

//                    Send(buffer);
//                }
//            }
//        }
//        private void CreateSendThread()
//        {
//            if (sendManual == null)
//            {
//                sendManual = new ManualResetEvent(false);
//            }
//            sendManual.Reset();
//            if (closeSendManual == null)
//            {
//                closeSendManual = new ManualResetEvent(false);
//            }
//            closeSendManual.Reset();
//            sendingThread = new Thread(new ThreadStart(BeginSendQueue));
//            sendingThread.IsBackground = IsBackground;
//            sendingThread.Start();
//        }
//        private void AddSplitQueue(byte[] bytes)
//        {
//            lock (splitePackQueue)
//            {
//                if (IsRuning == true)
//                {
//                    splitePackQueue.Enqueue(bytes);
//                }
//            }
//        }
//        private void AddQueue(byte[] bytes)
//        {
//            lock (queue)
//            {
//                if (IsRuning == true)
//                {
//                    queue.Enqueue(bytes);
//                    sendManual.Set();
//                }
//            }
//        }
//        private void BeginSendQueue()
//        {
//            while (true)
//            {
//                sendManual.WaitOne(5000,false);
//#if DEBUG
//                Console.WriteLine(DateTime.Now.ToString() + "发送线程正常");
//#endif
//                if(queue.Count>0)
//                {
//                      byte[] bytes = queue.Dequeue();
//                      SendData(bytes, 0, bytes.Length, null, false);
//                }
//                else
//                {
//                    if(splitePackQueue.Count>0)
//                    {
//                        byte[] bytes = splitePackQueue.Dequeue();
//                        SendData(bytes, 0, bytes.Length, null, false);
//                    }
//                }
//                if (closeSendManual.WaitOne(50, false))
//                {
//                    break;
//                }
//            }
//        }
//        private bool SendData(byte[] data, int offset, int size, object key, bool isAsync)
//        {
//            if (socket == null || !socket.Connected)
//                return false;

//            if (data.Length == 0)
//                return true;
//            try
//            {
//                if (isAsync)
//                {
//                    socket.BeginSend(data, offset, size, SocketFlags.None, new AsyncCallback(SendCallback), key);
//                }
//                else
//                {
//                    socket.Send(data, offset, size, SocketFlags.None);
//                    if (queue.Count == 0 && splitePackQueue.Count == 0)
//                    {
//                        sendManual.Reset();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Disconnect(ex.Message, DisconnectType.SendError);
//                return false;
//            }

//            return true;
//        }
//        private void SendCallback(IAsyncResult iar)
//        {
//            int len;
//            try
//            {
//                len = socket.EndSend(iar);
//                if (queue.Count == 0 && splitePackQueue.Count == 0)
//                {
//                    sendManual.Reset();
//                }
//            }
//            catch (Exception ex)
//            {
//                Disconnect(ex.Message, DisconnectType.SendError);
//                return;
//            }
//        }
//        protected void OnSocketDisconnecting(string message, DisconnectType dtype)
//        {
//            if (SocketDisconnecting != null && IsRuning==true)
//            {
//                IsRuning = false;
//                SocketDisconnecting(this,EventArgs.Empty);
//            }
//        }
//        private void Receive()
//        {
//            try
//            {
//                while (true)
//                {
//                    lastPackTime = DateTime.Now;
//                    int len = socket.Receive(receiveBuffer);
//                    if(len==0)
//                    {
//                        Disconnect("远程主机强制关掉了连接！", DisconnectType.ReadError);
//                        break;
//                    }
//                    byte[] newData = new byte[len];
//                    Array.Copy(receiveBuffer, 0, newData, 0, len);
//                    if (IsDecode == true)
//                    {
//                        try
//                        {
//                            dataPacketManager.Write(newData);
//                        }
//                        catch (Exception ex)
//                        {
//                            OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message });
//                        }
//                    }
//                    else
//                    {
//                        if (newData.Length > 0)
//                        {
//                            if (isWebSocket == true)
//                            {
//                                WriteData(newData);
//                            }
//                            else
//                            {
//                                OnDataAvailable(newData);
//                            } //OnDataAvailable(newData);
//                        }
//                    }
//                }
//            }
//            catch(Exception ex)
//            {
//                Disconnect(ex.Message, DisconnectType.ReadError);
//                return;
//            }
//        }
//        private void BeginReceive(bool isThread=false)
//        {
//            try
//            {
//                if (isThread == false)
//                {
        
//                    socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), receiveBuffer);
//                }
//                else
//                {
//                    Thread thread = new Thread(new ThreadStart(Receive));
//                    thread.IsBackground = IsBackground;
//                    thread.Start();
//                }
//            }
//            catch (Exception ex)
//            {
//                Disconnect("远程主机强制关掉了连接！", DisconnectType.ReadError);
//            }
//        }
//        private void OnDataAvailable(byte[] buffer)
//        {
//            if (DataAvailable != null && IsRuning==true)
//            {
//                DataAvailable(this, buffer);
//            }
//        }

//        /// <summary>
//        /// 异步接收数据回调。
//        /// </summary>
//        private void ReceiveCallback(IAsyncResult iar)
//        {
//            lastPackTime = DateTime.Now;
//            byte[] buffer = (byte[])iar.AsyncState;
//            int size = 0;
//            try
//            {
//                size = socket.EndReceive(iar);
//            }
//            catch (Exception ex)
//            {
//                if (IsRuning == true)
//                {
//                    Disconnect("远程主机强制关掉了连接！", DisconnectType.ReadError);
//                }
//                return;
//            }
//            if (size == 0)
//            {
//                Disconnect("远程主机强制关掉了连接！", DisconnectType.ReadError);
//                return;
//            }
//            else
//            {
//                byte[] newData = new byte[size];
//                Buffer.BlockCopy(buffer, 0, newData, 0, size);
//                if (IsDecode == true)
//                {
//                    try
//                    {
//                        dataPacketManager.Write(newData);
//                    }
//                    catch(Exception ex)
//                    {
//                        OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message });
//                    }
//                }
//                else
//                {
//                    if (isWebSocket == true)
//                    {
//                        WriteData(newData);
//                    }
//                    else
//                    {
//                        OnDataAvailable(newData);
//                    }
//                }
//                BeginReceive();
//            }
//        }
//        List<byte> bufferList = new List<byte>();
//        private void WriteData(byte[] recBytes)
//        {
//            lock (bufferList)
//            {
//                if (recBytes != null && recBytes.Length > 0)
//                    bufferList.AddRange(recBytes);
//                if (bufferList.Count < 6)
//                {
//                    return ;
//                }

//                bool fin = (bufferList[0] & 0x80) == 0x80; // 1bit，1表示最后一帧
//                if (!fin)
//                {
//                    return ;
//                }

//                bool mask_flag = (bufferList[1] & 0x80) == 0x80; // 是否包含掩码
//                if (!mask_flag)
//                {
//                    return ;// 不包含掩码的暂不处理
//                }
//                int payload_len = bufferList[1] & 0x7F; // 数据长度
//                if (bufferList.Count < payload_len + 6)
//                {
//                    return;
//                }
//                int offset = 0;
//                byte[] masks = new byte[4];
//                byte[] payload_data;
//                if (payload_len == 126)
//                {
//                    offset = 8;
//                    payload_len = (UInt16)(bufferList[2] << 8 | bufferList[3]);
//                    if (bufferList.Count < payload_len + offset)
//                    {
//                        return ;
//                    }
//                    bufferList.CopyTo(4, masks, 0, 4);
//                    payload_data = new byte[payload_len];
//                    bufferList.CopyTo(offset, payload_data, 0, payload_len);
//                    bufferList.RemoveRange(0, offset + payload_len);
//                    // Array.Copy(recBytes, 4, masks, 0, 4);

//                }
//                else if (payload_len == 127)
//                {
//                    bufferList.RemoveRange(0, 14 + payload_len);
//                    return;
//                }
//                else
//                {
//                    bufferList.CopyTo(2, masks, 0, 4);
//                    payload_data = new byte[payload_len];
//                    offset = 6;
//                    bufferList.CopyTo(offset, payload_data, 0, payload_len);
//                    bufferList.RemoveRange(0, offset + payload_len);
//                }

//                for (var i = 0; i < payload_len; i++)
//                {
//                    payload_data[i] = (byte)(payload_data[i] ^ masks[i % 4]);
//                }
//                OnDataAvailable(payload_data);
//                if(bufferList.Count!=0)
//                {
//                    WriteData(null);
//                }
//            }
//        }
//        private void OnRaiseErrored(RaiseErrorEvent e)
//        {
//            if (RaiseErrored != null && IsRuning==true)
//            {
//                RaiseErrored(this, e);
//            }
//        }
//        private DateTime lastPackTime=DateTime.Now;
//        private void dataPacketManager_DataPacketReceived(object sender, DataPacketBase e)
//        {
//            if(e is PoplarCloud.DataPacket.SplitDataPacket)
//            {
//                SplitPackManage(e as PoplarCloud.DataPacket.SplitDataPacket);
//                return;
//            }
//            if (DataPacketReceived != null && !(e is PoplarCloud.DataPacket.HeartPack) && IsRuning==true)
//            {
//                DataPacketReceived(this, e);
//            }
//        }
//        internal string Connect()
//        {
//            if (IsRuning == true)
//            {
//                return "";
//            }
//            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//            IPEndPoint iep = new IPEndPoint(IPAddress, this.Port);
//            try
//            {
//                socket.Connect(iep);
//                IsRuning = true;
//            }
//            catch (Exception ex)
//            {
//                return ex.Message;
//            }
//            dataPacketManager = new DataPacketManager();
//            dataPacketManager.DataPacketReceived += dataPacketManager_DataPacketReceived;
//            //Start();
//            BeginReceive();
//            CreateSendThread();
//            Thread.Sleep(100);
//            return "";
//        }
//        internal void BeginConnect(AsyncCallback callBack,object state)
//        {
//            beginConnectCompleteCallBack = callBack;
//            beginSender = state;
//            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//            IPEndPoint iep = new IPEndPoint(IPAddress, this.Port);
//            socket.BeginConnect(iep, requestCallBack, socket);
//        }
//        private void requestCallBack(IAsyncResult iar)
//        {
//            try
//            {
//                Socket client = (Socket)iar.AsyncState;
//                client.EndConnect(iar);
//                dataPacketManager = new DataPacketManager();
//                dataPacketManager.DataPacketReceived += dataPacketManager_DataPacketReceived;
//                BeginReceive();
//                CreateSendThread();
//                Thread.Sleep(200);
//                IsRuning = true;
//                if (beginConnectCompleteCallBack != null)
//                {
//                    beginConnectCompleteCallBack(new BeginConnectInfo(true,beginSender,this));
//                }
//            }
//            catch (Exception e)
//            {
//                if (beginConnectCompleteCallBack != null)
//                {
//                    beginConnectCompleteCallBack(new BeginConnectInfo(false, beginSender,null));
//                }
//            }
//        }
//        private void SplitPackManage(PoplarCloud.DataPacket.SplitDataPacket pack)
//        {
//            if(splitPackDic.ContainsKey(pack.SplitPackId)==false)
//            {
//                List<PoplarCloud.DataPacket.SplitDataPacket> list = new List<DataPacket.SplitDataPacket>();
//                list.Add(pack);
//                splitPackDic.Add(pack.SplitPackId, list);
//            }
//            else
//            {
//                List<PoplarCloud.DataPacket.SplitDataPacket> list = splitPackDic[pack.SplitPackId];
//                list.Add(pack);
//                if (pack.PackCount == list.Count)
//                {
//                    List<byte> bytelst = new List<byte>();
//                    foreach (PoplarCloud.DataPacket.SplitDataPacket datapack in list)
//                    {
//                        bytelst.AddRange(datapack.Data);
//                    }
//                    if(bytelst.Count==pack.PackLength)
//                    {
//                        dataPacketManager.NoListWrite(bytelst.ToArray());
//                    }
//                    lock(splitPackDic)
//                    {
//                        splitPackDic.Remove(pack.SplitPackId);
//                    }
//                }
//            }
//        }
//    }
//}
