using PoplarCloud.DataPacket;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace PoplarCloud.EventsAndEnum
{
    public delegate void IDataPacketHanler(IPacket pack);

    internal delegate void DataPackHandler(object sender, MessagePack e);
    /// <summary>
    /// 收到完整数据包时事件委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void DataPacketHandler(object sender, IPacket e);
    /// <summary>
    /// 发送失败的数据包
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="buffer"></param>
    public delegate void ErrorDataHandler(object sender,List<byte[]> buffer);
    /// <summary>
    /// 当有客户端连接成功时事件委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void SockeConnectedHandler(object sender, System.Net.Sockets.Socket e);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ConnectedHandler(object sender, YChannel e);
    /// <summary>
    /// 当有连接连接上线时发生
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void SocketConnectedHandler(object sender,Socket e);
    /// <summary>
    /// 当收到不进行编码数据时事件委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="buffer"></param>
    public delegate void DataAvailableHandler(object sender,byte[] buffer);
    /// <summary>
    /// 异步连接完成时回调委托
    /// </summary>
    /// <param name="state"></param>
    /// <param name="error"></param>
    public  delegate void BeginConnectCompleteCallBack(bool isComplete, string error,object state);
    public class BeginConnectInfo:IAsyncResult
    {
        public BeginConnectInfo(bool iscompleted,object state,string error="")
        {
            asyncState = state;
            isCompleted = iscompleted;
            errorMessage = error;
        }
        private string errorMessage = "";

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }
        private object asyncState;
        private bool isCompleted = false;
        public object AsyncState
        {
            get { return asyncState; }
        }

        public System.Threading.WaitHandle AsyncWaitHandle
        {
            get { return null; }
        }

        public bool CompletedSynchronously
        {
            get { return isCompleted; }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
        }
    }
    public class DisconnectEvent:EventArgs
    {
        public DisconnectEvent(DisconnectType dt,YChannel channel)
        {
            this.DisconnectType = dt;
            this.Channel = channel;
        }
        /// <summary>
        /// 断开类型
        /// </summary>
        public DisconnectType DisconnectType { get; private set; }
        /// <summary>
        /// 相关通道
        /// </summary>
        public YChannel Channel { get; private set; }
    }
    /// <summary>
    /// 错误信息类
    /// </summary>
    public class RaiseErrorEvent:EventArgs
    {
        private string errorMessage;

        public string ErrorMessage
        {
            get { return errorMessage; }
            internal set { errorMessage = value; }
        }
    }
}
