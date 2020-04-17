using PoplarCloud.DataPacket;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using PoplarCloud.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud
{
    public abstract class ServiceBase : DeviceBase
    {
        #region "字段"
        private List<YChannel> clientSocket;

        #endregion

        #region "属性"
        public List<YChannel> ClientSocket
        {
            get { return clientSocket; }
        }
        #endregion

        #region "事件"
        public event EventHandler ClientDisconnecting;
        /// <summary>
        /// 当客服端连接成功时
        /// </summary>
        public event EventHandler<YChannel> ClientConnected;
        #endregion
        public override void Init(string id)
        {
            this.Init(id, 3000);
        }
        /// <summary>
        /// 当收到来至客户端默认协议包时触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pack"></param>
        public abstract void DeviceDataPacketReceived(YChannel sender, IPacket pack);
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="id"></param>
        /// <param name="macConnect">最大连接数</param>
        public void Init(string id, int macConnect)
        {
            if (macConnect > 65500)
            {
                throw new Exception("无效的最大连接数！");
            }
            base.maxConnect = macConnect;
            base.Init(id);
            clientSocket = new List<YChannel>();
            transfer.Connected += transfer_Connected;
        }

        void transfer_Connected(object sender, System.Net.Sockets.Socket e)
        {
            YChannel client = new YChannel(e, socketPool, ChannelType.Client);
            if (socketPool.Count < 2)
            {
                return;
            }
            client.RaiseErrored += sm_RaiseErrored;
            client.SocketDisconnecting += sm_SocketDisconnecting;
            client.DataPacketReceived += sm_DataPacketReceived;
            client.DataPacketManager = this.Analysis;
            client.Start();
            if (CustomDecode == true)
            {
                NetNode node = new NetNode(client.Id, this.Id);
                this.NetTree.Add(node);
                OnConnected(client);
            }
        }
        public bool Start(uint port, uint maxListener)
        {
            return transfer.CreateService(port);
        }
        public bool Stop()
        {
            for (int i = ClientSocket.Count - 1; i >= 0; i--)
            {
                ClientSocket[i].Dispose();
            }
            return transfer.Stop();
        }
        protected virtual void OnConnected(YChannel e)
        {
            if (!clientSocket.Contains(e))
            {
                clientSocket.Add(e);
            }
            if (ClientConnected != null)
            {
                ClientConnected(this, e);
            }
        }

        void sm_DataPacketReceived(object sender, IPacket e)
        {
            if (e is MessagePack)
            {
                MessagePack mpack = e as MessagePack;
                if (mpack.MsgType == MessageType.Request)
                {
                    if (base.IsConnect == true)
                    {
                        this.Send(mpack);
                    }
                    else
                    {
                        DeviceDataPacketReceived((YChannel)sender, mpack);
                    }
                    return;
                }
                if (mpack.TargetId.Equals(this.Id))
                {
                    DeviceDataPacketReceived((YChannel)sender, mpack);
                }
                else
                {
                    this.Send(mpack);
                }
                return;
            }
            if (e is SocketRegisterPacket)
            {
                SocketRegisterPacket spack = e as SocketRegisterPacket;
                YChannel client = sender as YChannel;
                switch (spack.RType)
                {
                    case RegisterType.Add:
                        NetNode node = null;
                        try
                        {
                            node = JsonConvert.DeserializeObject<NetNode>(spack.Data);
                        }
                        catch { return; }
                        if (node != null)
                        {
                            if (string.IsNullOrWhiteSpace(node.ParentId))
                            {
                                node.ParentId = this.Id;
                            }
                            try
                            {
                                base.NetTree.Add(node);
                            }
                            catch (Exception ex)
                            {
                                client.Send(new SocketRegisterPacket(RegisterType.CallBack, this.Id, ex.Message));
                                OnRaiseError(ex.Message);
                                return;
                            }
                            if (node.ParentId != this.Id && base.IsConnect == true)
                            {
                                base.ParentSocket.Send(spack);
                            }
                        }
                        if (client != null)
                        {
                            client.Send(new SocketRegisterPacket(RegisterType.CallBack, this.Id));
                            client.Id = node.Id;
                            OnConnected(client);
                        }
                        break;
                    case RegisterType.CallBack: break;
                    case RegisterType.Delete:
                        NetNode dnode = null;
                        try
                        {
                            dnode = JsonConvert.DeserializeObject<NetNode>(spack.Data);
                        }
                        catch { return; }
                        if (dnode != null)
                        {
                            base.NetTree.Remove(dnode);
                            if (dnode.ParentId != this.Id && base.IsConnect == true)
                            {
                                base.ParentSocket.Send(spack);
                            }
                        }

                        break;
                }
                return;
            }
            DeviceDataPacketReceived((YChannel)sender, e);
        }

        void sm_RaiseErrored(object sender, RaiseErrorEvent e)
        {
            OnRaiseError(e.ErrorMessage);
        }
        void sm_SocketDisconnecting(object sender, EventArgs e)
        {
            YChannel channel = sender as YChannel;
            NetNode node = NetTree.FindNode(channel.Id);
            clientSocket.Remove(channel);
            if (node != null)
            {
                base.NetTree.Remove(node);
                if (base.IsConnect == true)
                {
                    SocketRegisterPacket pack = new SocketRegisterPacket(RegisterType.Delete, JsonConvert.SerializeObject(node));
                    base.ParentSocket.Send(pack);
                }
            }
            if (ClientDisconnecting != null)
            {
                ClientDisconnecting(sender, e);
            }
        }
        public void Send(YChannel chanael, IPacket pack)
        {
            if (chanael == null)
            {
                OnRaiseError("通道不能为空");
                return;
            }
            chanael.Send(pack.Encoder());
        }
        public override void Send(IPacket ipack)
        {
            if (!(ipack is MessagePack))
            {
                OnRaiseError("此方法只适用于默认协议解析，自定义协议请选择指定通道发送");
                return;
            }
            MessagePack pack = ipack as MessagePack;
            if (pack.TargetId == this.Id)
            {
                DeviceDataPacketReceived(null, pack);
                return;
            }
            YChannel channel = null;

            string nextId = base.NetTree.FintNextNode(this.Id, pack.TargetId);
            if (string.IsNullOrEmpty(nextId))
            {
                if (pack.MsgType == MessageType.Back)
                {
                    return;
                }
                if (this.ParentSocket == null)
                {
                    if (!pack.FromId.Equals(this.Id))
                    {
                        MessagePack backPack = new MessagePack(pack.FromId, MessageType.Error, Encoding.UTF8.GetBytes("未找到该设备"));
                        this.Send(backPack);
                    }
                    OnRaiseError("未找到目标");
                }
                else
                {
                    base.Send(pack);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(pack.FromId))
                {
                    pack.FromId = this.Id;
                }
                channel = Find(nextId);
                if (channel != null)
                {
                    channel.Send(pack.Encoder());
                    return;
                }
            }
        }
        public YChannel Find(string id)
        {
            return this.clientSocket.FirstOrDefault(p => p.Id == id);
        }
        public YChannel Find(string ip, int port)
        {
            return Find(string.Format("{0}:{1}", ip, port));
        }
    }
}
