using PoplarCloud.DataPacket;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PoplarCloud
{
	/// <summary>
	/// 通道
	/// </summary>
	public class YChannel : EventArgs, IDisposable
	{
		/// <summary>
		/// 本地socket
		/// </summary>
		private Socket socket;
		private SocketAsyncEventArgsPool socketPool;//iocp池
		private SocketAsyncEventArgs readArgs;//读取
		private bool isRuning = false;

		public ChannelType ChannelType;
		private bool isConnect = false;

		public bool IsConnect
		{
			get { return isConnect; }
			private set { isConnect = value; }
		}
		private IAnalysis dataPacketManager;

		public IAnalysis DataPacketManager
		{
			get { return dataPacketManager; }
			set
			{
				if (value == null)
				{
					dataPacketManager = new YDataPacketManager();
					dataPacketManager.DataPacketReceived += dataPacketManager_DataPacketReceived;
					dataPacketManager.InvalidPacketReceived += dataPacketManager_InvalidPacketReceived;
				}
				else
				{
					dataPacketManager = value;
					dataPacketManager.DataPacketReceived += dataPacketManager_DataPacketReceived;
					dataPacketManager.InvalidPacketReceived += dataPacketManager_InvalidPacketReceived;
				}
			}
		}

		void dataPacketManager_InvalidPacketReceived(object sender, EventArgs e)
		{
		}
		private string address = "";

		private int port = 0;

		private ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
		/// <summary>
		/// 连接地址
		/// </summary>
		public string Address
		{
			get { return address; }
		}
		/// <summary>
		/// 端口
		/// </summary>
		public int Port
		{
			get { return port; }
		}
		///// <summary>
		///// 是否开启自定义编码协议
		///// 如果值为true，DataAvailable事件将启用，DataPacketReceived则无效。
		///// </summary>
		//public bool CustomDecode
		//{
		//    get { return customDecode; }
		//    internal set { customDecode = value; }
		//}
		private uint keepTime = 3000;

		public uint KeepTime
		{
			get { return keepTime; }
			internal set { keepTime = value; }
		}
		private string id;
		/// <summary>
		/// 通道Id用于登录用户
		/// </summary>
		public string Id
		{
			get { return id; }
			internal set { id = value; }
		}
		/// <summary>
		/// 当连接断开时发生
		/// </summary>
		public event EventHandler<DisconnectEvent> SocketDisconnecting;
		/// <summary>
		/// 当收到完整数据包时发生（CustomDecode==fasle时有效,默认启用）
		/// </summary>
		internal event DataPacketHandler DataPacketReceived;
		/// <summary>
		/// 当有异常发生时
		/// </summary>
		public event EventHandler<RaiseErrorEvent> RaiseErrored;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="socketPool"></param>
		/// <param name="id">通道Id</param>
		internal YChannel(Socket socket, SocketAsyncEventArgsPool socketPool, ChannelType ct, string id = "")
		{
			if (socket == null)
			{
				return;
			}
			this.ChannelType = ct;
			SetKeepAliveValues(socket, true, KeepTime, KeepTime * 2);
			this.socket = socket;
			address = (((IPEndPoint)socket.RemoteEndPoint).Address).ToString();
			port = ((IPEndPoint)socket.RemoteEndPoint).Port;
			this.Id = string.IsNullOrWhiteSpace(id) ? string.Format("{0}:{1}", address, port) : id;
			this.socketPool = socketPool;
			dataPacketManager = new YDataPacketManager();
			dataPacketManager.DataPacketReceived += dataPacketManager_DataPacketReceived;
		}
		internal void SetSocket(Socket socket, SocketAsyncEventArgsPool socketPool)
		{
			if (socket == null)
			{
				return;
			}
			this.socket = socket;
			SetKeepAliveValues(socket, true, KeepTime, KeepTime * 2);
			address = (((IPEndPoint)this.socket.RemoteEndPoint).Address).ToString();
			port = ((IPEndPoint)this.socket.RemoteEndPoint).Port;
			this.socketPool = socketPool;
		}
		private YChannel()
		{

		}
		/// <summary>
		/// 开启收接
		/// </summary>
		public void Start()
		{
			if (socketPool == null)
			{
				return;
			}
			readArgs = socketPool.Pop();
			readArgs.UserToken = socket;
			readArgs.Completed -= readArgs_Completed;
			readArgs.Completed += readArgs_Completed;
			BeginReceive(readArgs);
			if (sendQueue.Count != 0)
			{
				StartSend();
			}
			IsConnect = true;
			isRuning = true;
		}
		/// <summary>
		/// 发送一条数据包
		/// </summary>
		/// <param name="buffer"></param>
		internal void Send(byte[] buffer)
		{
			sendQueue.Enqueue(buffer);
			StartSend();
		}
		private void StartSend()
		{
			SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
			sendArgs.UserToken = socket;
			sendArgs.Completed += readArgs_Completed;
			BeginSend(sendArgs);
		}
		internal void Send(DataPacketBase pack)
		{
			try
			{
				this.Send(pack.Encoder());
			}
			catch (Exception ex)
			{
				OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message });
			}
		}
		private static object lockobj = new object();
		/// <summary>
		/// 停止收发并关闭socket
		/// </summary>
		public void Close(DisconnectType e = DisconnectType.Manual)
		{
			lock (lockobj)
			{
				if (isRuning == true)
				{
					isRuning = false;
					try
					{
						socket.Shutdown(SocketShutdown.Send);
					}
					catch (Exception) { }
					try
					{
						socket.Shutdown(SocketShutdown.Receive);
					}
					catch (Exception) { }
					socket.Close();
					if (socketPool != null)
					{
						if (readArgs != null)
						{
							readArgs.Completed -= readArgs_Completed;
							socketPool.Push(readArgs);
							readArgs.UserToken = null;
						}
					}
					socket = null;
					OnSocketDisconnecting(new DisconnectEvent(e, this));
				}
				IsConnect = false;
			}
		}
		/// <summary>
		/// IOCP完成操作时发生
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void readArgs_Completed(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					if (e.SocketError == SocketError.Success)
					{
						if (e.BytesTransferred <= 0)
						{
							Close(DisconnectType.NetError);
							return;
						}
						byte[] newData = new byte[e.BytesTransferred];
						Array.Copy(e.Buffer, e.Offset, newData, 0, e.BytesTransferred);
						try
						{
							dataPacketManager.ReceivedData(newData);
						}
						catch (Exception ex)
						{
							OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = ex.Message });
						}
						BeginReceive(e);
					}
					else
					{
						Close(DisconnectType.Disconnect);
					}
					break;
				case SocketAsyncOperation.Send:
					if (e.SocketError != SocketError.Success)
					{
						sendQueue.Enqueue(e.Buffer);
						//e.Buffer;
						Close(DisconnectType.NetError);
					}
					else
					{
						StartSend();
					}
					break;
				default:
					OnRaiseErrored(new RaiseErrorEvent() { ErrorMessage = "读写错误" }); break;
			}

		}

		private void BeginSend(SocketAsyncEventArgs e)
		{
			if (e.UserToken == null)
			{
				return;
			}
			if (this.socket != null)
			{
				byte[] buffer;
				if (sendQueue.TryDequeue(out buffer))
				{
					e.SetBuffer(buffer, 0, buffer.Length);
					if (!this.socket.SendAsync(e))
					{
						BeginSend(e);
						return;
					}
					//sendQueue.TryDequeue(out buffer);
				}
			}
		}
		private void BeginReceive(SocketAsyncEventArgs e)
		{
			if (this.socket == null)
			{
				return;
			}
			if (!this.socket.ReceiveAsync(e))
			{
				BeginReceive(e);
			}
		}
		private void OnRaiseErrored(RaiseErrorEvent raiseErrorEvent)
		{
			if (RaiseErrored != null)
			{
				RaiseErrored(this, raiseErrorEvent);
			}
		}
		private void OnSocketDisconnecting(DisconnectEvent e)
		{
			if (SocketDisconnecting != null)
			{
				SocketDisconnecting(this, e);
			}
		}
		void dataPacketManager_DataPacketReceived(object sender,IPacket e)
		{
			if (DataPacketReceived != null)
			{
				DataPacketReceived(this, e);
			}
		}


		[
			System.Runtime.InteropServices.StructLayout
			(
				System.Runtime.InteropServices.LayoutKind.Explicit
			)
		]
		unsafe struct TcpKeepAlive
		{
			[System.Runtime.InteropServices.FieldOffset(0)]
			[
				System.Runtime.InteropServices.MarshalAs
				(
					System.Runtime.InteropServices.UnmanagedType.ByValArray,
					SizeConst = 12
				)
			]
			public fixed byte Bytes[12];

			[System.Runtime.InteropServices.FieldOffset(0)]
			public uint On_Off;

			[System.Runtime.InteropServices.FieldOffset(4)]
			public uint KeepaLiveTime;

			[System.Runtime.InteropServices.FieldOffset(8)]
			public uint KeepaLiveInterval;
		}

		private int SetKeepAliveValues
			(
				System.Net.Sockets.Socket socket,
				bool On_Off,
				uint KeepaLiveTime,
				uint KeepaLiveInterval
			)
		{
			int Result = -1;

			unsafe
			{
				TcpKeepAlive KeepAliveValues = new TcpKeepAlive();

				KeepAliveValues.On_Off = Convert.ToUInt32(On_Off);
				KeepAliveValues.KeepaLiveTime = KeepaLiveTime;
				KeepAliveValues.KeepaLiveInterval = KeepaLiveInterval;

				byte[] InValue = new byte[12];

				for (int I = 0; I < 12; I++)
					InValue[I] = KeepAliveValues.Bytes[I];
				Result = socket.IOControl(IOControlCode.KeepAliveValues, InValue, null);
			}

			return Result;
		}
		public void Dispose()
		{
			byte[] buffer;
			while (sendQueue.TryDequeue(out buffer))
			{
				Thread.Sleep(1);
			}
			this.Close(DisconnectType.Manual);
		}
	}
}
