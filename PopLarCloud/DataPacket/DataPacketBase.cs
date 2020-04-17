
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PoplarCloud.DataPacket
{
    /// <summary>
    /// 数据包基类
    /// </summary>
    public abstract class DataPacketBase : IPacket
    {
        internal string PacketType { get; set; }
        /// <summary>
        /// 打包数据
        /// </summary>
        /// <returns></returns>
        public byte[] Encoder()
        {
            PacketType = this.GetType().Name;
            BinaryWriter bw = CreateStrame();
            EncoderData(bw);
            return CloseStrame(bw);
        }

        public bool Decoder(object obj)
        {
            BinaryReader br = obj as BinaryReader;
            if(br==null)
            {
                return false;
            }
            bool success = false;
            try
            {
                DecoderData(br);
                success = true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("{0} 解包出错：{1}", this.GetType().Name, ex.Message));
                //string errMsg = String.Format("{0} 解包出错：{1}", this.GetType().Name, ex.Message);
            }
            br.Close();
            return success;
        }

        public void Decoder<BinaryReader>(BinaryReader br)
        {
            //throw new NotImplementedException();
        }
        protected abstract void EncoderData(BinaryWriter bw);
        /// <summary>
        /// 解包数据
        /// </summary>
        public virtual bool Decoder(BinaryReader br)
        {
            bool success = false;
            try
            {
                DecoderData(br);
                success = true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("{0} 解包出错：{1}", this.GetType().Name, ex.Message));
                //string errMsg = String.Format("{0} 解包出错：{1}", this.GetType().Name, ex.Message);
            }
            br.Close();
            return success;
        }
        protected abstract void DecoderData(BinaryReader br);
        /// <summary>
        /// 将流长度写入到流的起始位置并关闭数据包内存流。
        /// </summary>
        /// <returns>包含该流数据的 Byte[]。</returns>
        private byte[] CloseStrame(BinaryWriter bw)
        {
            
            bw.BaseStream.Seek(0, SeekOrigin.Begin);
            bw.Write((int)bw.BaseStream.Length);		//写入包长度。
            byte[] buffer = ((MemoryStream)bw.BaseStream).ToArray();
            bw.Close();

            return buffer;
        }
        private BinaryWriter CreateStrame()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8);
            bw.Write(0);	//为包长度预留位置。
            bw.Write(PacketType);
            return bw;
        }



 
    }
}
