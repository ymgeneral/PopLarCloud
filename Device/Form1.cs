using PoplarCloud;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using PoplarCloud.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Device
{
    public partial class Form1 : Form
    {
        private Device device;
        public Form1()
        {
            InitializeComponent();
            device = new Device();
            device.DataPackReceived += device_DataPackReceived;

			device.RaiseError += device_RaiseError;
            device.ParentDisconnecting += device_ParentDisconnecting;
            device.IsReconnection = true;
           
           // device.Analysis = new CustomPackManager();//表示自定义协议，如果不写使用默认协议
            device.Init("11111");
            device.BeginConnectComplete += device_BeginConnectComplete;
            //transfer = new Transfer();
            //transfer.Connected += transfer_Connected;
        }

        void device_ParentDisconnecting(object sender, EventArgs e)
        {
            WriteText("与服务器断开连接");
        }

        void device_RaiseError(object sender, RaiseErrorEvent e)
        {
            WriteText(e.ErrorMessage);
        }

        void device_DataPackReceived(object sender, IPacket e)
        {

            //CustomPack pack = e as CustomPack;
            //WriteText(pack.Text);

			///以下是默认协议包
			PoplarCloud.DataPacket.MessagePack pack = e as PoplarCloud.DataPacket.MessagePack;
			if (pack.MsgType == MessageType.Msg)
			{
				WriteText(Encoding.UTF8.GetString(pack.Data));
			}
			//if (pack.MsgType == MessageType.Back)
			//{
			//	string str = Encoding.UTF8.GetString(pack.Data);
			//	List<NetNode> lst = JsonConvert.DeserializeObject<List<NetNode>>(str);
			//	MessageBox.Show(str + "   " + lst.Count);
			//}
		}
        private void WriteText(string text)
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action<string>(WriteText), text);
                return;
            }
            richTextBox1.AppendText(string.Format("【{0}】：{1}\r\n", DateTime.Now.ToString("HH:mm:ss"), text));
        }
        void transfer_Connected(object sender, System.Net.Sockets.Socket e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            device.BeginConnect("127.0.0.1", 8889, null);
        }

        void device_BeginConnectComplete(bool isComplete, string error, object state)
        {
            WriteText("连接：" + isComplete);
        }

        private void button2_Click(object sender, EventArgs e)
        {

			if (!string.IsNullOrEmpty(textBox1.Text))
            {
				for (int i = 0; i < 1000; i++)
				{
					device.Send(new PoplarCloud.DataPacket.MessagePack(
																device.ParentSocket.Id,
																MessageType.Msg,
																Encoding.UTF8.GetBytes(i.ToString())));
				}

				//device.Send(new CustomPack() { Text = textBox1.Text });
            }
            //transfer.ConnectParent("127.0.0.1", 8889);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            device.CloseConnect();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            device.Dispose();
        }

    }
}
