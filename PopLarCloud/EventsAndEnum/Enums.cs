using System;
using System.Collections.Generic;
using System.Text;

namespace PoplarCloud.EventsAndEnum
{
    /// <summary>
    /// Transfer形态
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        /// 服务器
        /// </summary>
        Server = 0x1,
        /// <summary>
        /// 客户端
        /// </summary>
        Client = 0x2,
    }
    /// <summary>
    /// 监听状态
    /// </summary>
    internal enum ListenerState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle,
        /// <summary>
        /// 初始化状态
        /// </summary>
        Loaded,
        /// <summary>
        /// 运行状态
        /// </summary>
        Runing
    }
    public  enum MessageType : byte
    {
        /// <summary>
        /// 正常消息
        /// </summary>
        Msg,
        /// <summary>
        /// 返回信息
        /// </summary>
        Back,
        /// <summary>
        /// 错误
        /// </summary>
        Error,
        /// <summary>
        /// 获取请求
        /// </summary>
        Request
    }
    /// <summary>
    /// 断链信息
    /// </summary>
    public enum DisconnectType
    {
        /// <summary>
        /// 重新连接。
        /// </summary>
        Reconnect,

        /// <summary>
        /// 用户执行断开操作。
        /// </summary>
        Manual,

        /// <summary>
        /// 对方断开连接。
        /// </summary>
        Disconnect,

        /// <summary>
        /// 等待欢迎、Socket用途或服务器就绪信息超时。
        /// </summary>
        Timeout,
        /// <summary>
        /// 网络通信错误。
        /// </summary>
        NetError,

        ///// <summary>
        ///// 解析DNS失败。
        ///// </summary>
        //ResolveDnsFail,

        ///// <summary>
        ///// 版本错误。
        ///// </summary>
        ///// <remarks>例如欢迎信息不匹配等。</remarks>
        //VersionError,

        /// <summary>
        /// 系统操作断开。
        /// </summary>
        /// <remarks>例如系统关闭。</remarks>
        System
    }
    public enum RegisterType
    {
        Add,
        Delete,
        CallBack
    }
    /// <summary>
    /// 错误信息枚举
    /// </summary>
    public enum ErrorType
    {
        IpError,
        PortError,
        ConnectError,
        /// <summary>
        /// 发送数据包错误
        /// </summary>
        SendError,
        /// <summary>
        /// 读取数据包错误
        /// </summary>
        ReadError,
        ListenError
    }
}
