///Websockt部分由于整个框架修改成了iocp后未计算更新，后续修改


//using PoplarCloud;
//using PoplarCloud.EventsAndEnum;
//using System;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Security.Cryptography;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace PoplarCloud
//{
//    public class WebService
//    {
//        private byte[] receiveBuffer = new byte[1024];
//        private Dictionary<string, object> registerDic = new Dictionary<string, object>();
//        /// <summary>
//        /// 已经注册的客户端
//        /// </summary>
//        public Dictionary<string, object> RegisterDic
//        {
//            get { return registerDic; }
//            set { registerDic = value; }
//        }
//        Transfer transfer;
//        List<YChannel> connClient = new List<YChannel>();
//        /// <summary>
//        /// 与服务器断开连接事件
//        /// </summary>
//        public event DisconnectHandler ServiceSocketDisconnect;
//        /// <summary>
//        /// 注册完成时发生
//        /// </summary>
//        public event EventHandler RegisterComplete;
//        /// <summary>
//        /// 异常
//        /// </summary>
//        public event EventHandler<ErrorEvent> RaiseErrored;
//        public event DataAvailableHandler DataAvailableAction;
//        public WebService()
//        {
//            transfer = new Transfer(500);
//            transfer.Connected += transfer_Connected;
//            transfer.RaiseErrored += Transfer_RaiseErrored; ;
//        }

//        private void Transfer_RaiseErrored(object sender, RaiseErrorEvent e)
//        {
//            if (RaiseErrored != null)
//            {
//                ErrorEvent ee = new ErrorEvent();
//                ee.ErrorMessage = e.ErrorMessage;
//                ee.ErrorType = (DeviceService.ErrorType)e.ErrorType;
//                RaiseErrored(this, ee);
//            }
//        }

//        protected void OnRaiseErrored(ErrorEvent e)
//        {
//            if (RaiseErrored != null)
//            {
//                RaiseErrored(this, e);
//            }
//        }
//        private void OnServiceSocketDisconnect(SocketDisconnectEvent e)
//        {
//            if (ServiceSocketDisconnect != null)
//            {
//                DisconnectEvent de = new DisconnectEvent();
//                de.DisconnectType = (DeviceService.DisconnectType)e.DisconnectType;
//                de.Guid = e.Guid;
//                de.IP = e.NetAddress;
//                de.Message = e.Message;
//                ServiceSocketDisconnect(this, de);
//            }
//        }
//        void transfer_Connected(object sender, System.Net.Sockets.Socket e)
//        {
//            try
//            {
//                YChannel socket = new YChannel(e,true) { IsDecode = false, ServiceHeartBeat = false };
              
//                socket.RaiseErrored += Transfer_RaiseErrored;
//                socket.SocketDisconnecting += socket_SocketDisconnecting;
//                socket.DataAvailable += socket_DataAvailable;
//                socket.Strat();
//                connClient.Add(socket);
//                if (RegisterComplete != null)
//                {
//                    RegisterComplete(sender, EventArgs.Empty);
//                }
//            }
//            catch { }
//        }
//        void socket_DataAvailable(object sender, byte[] buffer)
//        {
//            if(buffer.Length==2)
//            {
//                if(buffer[0]==3&& buffer[1]==233)
//                {
//                    YChannel socket = sender as YChannel;
//                    if(socket!=null)
//                    {
//                        socket.Dispose();
//                        return;
//                    }
//                }
//            }
//            string text = Encoding.UTF8.GetString(buffer);
//            if (DataAvailableAction != null && text!=string.Empty)
//            {
//                DataAvailableAction(sender, text);
//            }
//        }
 
//        void socket_SocketDisconnecting(YChannel sender, EventsAndEnum.SocketDisconnectEvent e)
//        {
//            connClient.Remove(sender);
//            if (registerDic.ContainsValue(sender) == true)
//            {
//                string key = "";
//                foreach (KeyValuePair<string, object> item in registerDic)
//                {
//                    if (item.Value.Equals(sender))
//                    {
//                        key = item.Key;
//                        break;
//                    }
//                }
//                if (!string.IsNullOrEmpty(key))
//                {
//                    registerDic.Remove(key);
//                }
//            }
            
//            OnServiceSocketDisconnect(e);
//        }
//        public bool Start(uint port, uint maxListener)
//        {
//            return transfer.CreateService(port, maxListener,true);
//        }
//        public bool Send(string text,object socketobj)
//        {
//            SocketManager socket = socketobj as SocketManager;
//            if (socket != null)
//            {
//                socket.Send(PackData(text));
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }
//        public bool CloseSocket(object obj)
//        {
//            SocketManager socket = obj as SocketManager;
//            if (socket != null)
//            {
//                socket.Dispose();
//                foreach (KeyValuePair<string, object> kvp in RegisterDic)
//                {
//                    if (kvp.Value.Equals(socket))
//                    {
//                        registerDic.Remove(kvp.Key);
//                        break;
//                    }
//                }
//            }
//            return true;
//        }
//        /// <summary>
//        /// 解析数据包
//        /// </summary>
//        /// <param name="recBytes"></param>
//        /// <param name="recByteLength"></param>
//        /// <returns></returns>
//        //private string AnalyticData(byte[] recBytes, int recByteLength)
//        //{

//        //    lock (bufferList)
//        //    {
//        //        if (recBytes != null && recBytes.Length > 0)
//        //            bufferList.AddRange(recBytes);
//        //        if (bufferList.Count < 6)
//        //        {
//        //            return "";
//        //        }

//        //        bool fin = (bufferList[0] & 0x80) == 0x80; // 1bit，1表示最后一帧
//        //        if (!fin)
//        //        {
//        //            return string.Empty;
//        //        }

//        //        bool mask_flag = (bufferList[1] & 0x80) == 0x80; // 是否包含掩码
//        //        if (!mask_flag)
//        //        {
//        //            return string.Empty;// 不包含掩码的暂不处理
//        //        }
//        //        int payload_len = recBytes[1] & 0x7F; // 数据长度
//        //        if (bufferList.Count < payload_len + 6)
//        //        {
//        //            return "";
//        //        }
//        //        int offset = 0;
//        //        byte[] masks = new byte[4];
//        //        byte[] payload_data;
//        //        if(payload_len==126)
//        //        {
//        //            offset = 8;
//        //            payload_len = (UInt16)(bufferList[2] << 8 | bufferList[3]);
//        //            if(bufferList.Count<payload_len+offset)
//        //            {
//        //                return "";
//        //            }
//        //            bufferList.CopyTo(4, masks, 0, 4);
//        //            payload_data = new byte[payload_len];
//        //            bufferList.CopyTo(offset, payload_data, 0, payload_len);
//        //            bufferList.RemoveRange(0, offset + payload_len);
//        //            // Array.Copy(recBytes, 4, masks, 0, 4);
                   
//        //        }
//        //        else if(payload_len==127)
//        //        {
//        //        }
//        //        else
//        //        {
//        //            bufferList.CopyTo(2, masks, 0, 4);
//        //            payload_data = new byte[payload_len];
//        //            offset = 6;
//        //            bufferList.CopyTo(offset, payload_data, 0, payload_len);
//        //            bufferList.RemoveRange(0, offset + payload_len);
//        //        }

//        //        for (var i = 0; i < payload_len; i++)
//        //        {
//        //            payload_data[i] = (byte)(payload_data[i] ^ masks[i % 4]);
//        //        }
//        //    }
//        //    //if (recByteLength <= 2) { return string.Empty; }

//        //    //bool fin = (recBytes[0] & 0x80) == 0x80; // 1bit，1表示最后一帧
//        //    //if (!fin)
//        //    //{
//        //    //    return string.Empty;
//        //    //}

//        //    //bool mask_flag = (recBytes[1] & 0x80) == 0x80; // 是否包含掩码
//        //    //if (!mask_flag)
//        //    //{
//        //    //    return string.Empty;// 不包含掩码的暂不处理
//        //    //}

//        //    //int payload_len = recBytes[1] & 0x7F; // 数据长度
//        //    //int offset = 0;
//        //    //byte[] masks = new byte[4];
//        //    //byte[] payload_data;

//        //    //if (payload_len == 126)
//        //    //{
//        //    //    Array.Copy(recBytes, 4, masks, 0, 4);
//        //    //    payload_len = (UInt16)(recBytes[2] << 8 | recBytes[3]);
//        //    //    payload_data = new byte[payload_len];
//        //    //    offset = 8;
//        //    //    Array.Copy(recBytes, offset, payload_data, 0, payload_len);

//        //    //}
//        //    //else if (payload_len == 127)
//        //    //{
//        //    //    Array.Copy(recBytes, 10, masks, 0, 4);
//        //    //    byte[] uInt64Bytes = new byte[8];
//        //    //    for (int i = 0; i < 8; i++)
//        //    //    {
//        //    //        uInt64Bytes[i] = recBytes[9 - i];
//        //    //    }
//        //    //    UInt64 len = BitConverter.ToUInt64(uInt64Bytes, 0);

//        //    //    payload_data = new byte[len];
//        //    //    for (UInt64 i = 0; i < len; i++)
//        //    //    {
//        //    //        payload_data[i] = recBytes[i + 14];
//        //    //    }
//        //    //    offset = 14;
//        //    //}
//        //    //else
//        //    //{
//        //    //    Array.Copy(recBytes, 2, masks, 0, 4);
//        //    //    payload_data = new byte[payload_len];
//        //    //    offset = 6;
//        //    //    Array.Copy(recBytes, offset, payload_data, 0, payload_len);

//        //    //}

//        //    //return Encoding.UTF8.GetString(payload_data);
//        //}
//        /// <summary>
//        /// 打包数据
//        /// </summary>
//        /// <param name="message"></param>
//        /// <returns></returns>
//        private static byte[] PackData(string message,bool ismsg=false)
//        {
//            if(ismsg==false)
//            {
//                return PackData(message);
//            }
//            byte[] contentBytes = null;
//            byte[] temp = Encoding.UTF8.GetBytes(message);
//            byte[] msg = new byte[4] { 0x01, 0x04, 0x02, 0x02 };
//            if (temp.Length < 126)
//            {
//                contentBytes = new byte[temp.Length + 6];
//                contentBytes[0] = 0x81;
//                contentBytes[1] = (byte)(temp.Length | 0x80);
//                contentBytes[2] = msg[0];
//                contentBytes[3] = msg[1];
//                contentBytes[4] = msg[2];
//                contentBytes[5] = msg[3];
//                for (int i = 0; i < temp.Length;i++ )
//                {
//                    temp[i] = (byte)(temp[i] ^ msg[i % 4]);
//                }
//                Array.Copy(temp, 0, contentBytes, 6, temp.Length);
//            }
//            else if (temp.Length < 0xFFFF)
//            {
//                contentBytes = new byte[temp.Length + 8];
//                contentBytes[0] = 0x81;
//                contentBytes[1] = 126;
//                contentBytes[2] = (byte)(temp.Length & 0xFF);
//                contentBytes[3] = (byte)(temp.Length >> 8 & 0xFF);
//                contentBytes[4] = msg[0];
//                contentBytes[5] = msg[1];
//                contentBytes[6] = msg[2];
//                contentBytes[7] = msg[3];
//                for (int i = 0; i < temp.Length; i++)
//                {
//                    temp[i] = (byte)(temp[i] ^ msg[i% 4]);
//                }
//                Array.Copy(temp, 0, contentBytes, 8, temp.Length);
//            }
//            else
//            {
//                // 暂不处理超长内容
//            }

//            return contentBytes;
//        }
//        private static byte[] PackData(string message)
//        {
//            byte[] contentBytes = null;
//            byte[] temp = Encoding.UTF8.GetBytes(message);

//            if (temp.Length < 126)
//            {
//                contentBytes = new byte[temp.Length + 2];
//                contentBytes[0] = 0x81;
//                contentBytes[1] = (byte)temp.Length;
//                Array.Copy(temp, 0, contentBytes, 2, temp.Length);
//            }
//            else if (temp.Length < 0xFFFF)
//            {
//                contentBytes = new byte[temp.Length + 4];
//                contentBytes[0] = 0x81;
//                contentBytes[1] = 126;
//                contentBytes[2] = (byte)(temp.Length & 0xFF);
//                contentBytes[3] = (byte)(temp.Length >> 8 & 0xFF);
//                Array.Copy(temp, 0, contentBytes, 4, temp.Length);
//            }
//            else
//            {
//                // 暂不处理超长内容
//            }

//            return contentBytes;
//        }
//        public bool Stop()
//        {
//           if(transfer!=null)
//           {
//               transfer.Stop();
//           }
//           registerDic.Clear();
           
//           foreach (SocketManager socket in connClient)
//           {
//               try
//               {
//                   socket.SocketDisconnecting -= socket_SocketDisconnecting;
//                   socket.Dispose();
//               }
//               catch { }
//           }
//           connClient.Clear();
//           return true;
//        }
//    }
//}
