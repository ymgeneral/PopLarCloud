using PoplarCloud;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Service
{
    public partial class Form1 : Form
    {
        private Service service;
        public Form1()
        {
            InitializeComponent();
            service = new Service();
            service.ClientConnected += service_ClientConnected;
            service.ClientDisconnecting += service_ClientDisconnecting;
            //service.Analysis = new CustomPackManager();
            service.DataPackReceived += service_DataPackReceived;
            service.Init("111");
        }

        void service_ClientDisconnecting(object sender, EventArgs e)
        {
            SetList();
        }

        void service_DataPackReceived(object sender, IPacket e)
        {
            //CustomPack pack = e as CustomPack;
            //WriteText(pack.Text);
			PoplarCloud.DataPacket.MessagePack pack = e as PoplarCloud.DataPacket.MessagePack;
			if (pack.MsgType == MessageType.Msg)
			{
				WriteText(Encoding.UTF8.GetString(pack.Data));
			}
		}
        private void WriteText(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(WriteText), text);
                return;
            }
            richTextBox1.AppendText(string.Format("【{0}】：{1}\r\n", DateTime.Now.ToString("HH:mm:ss"), text));
        }
        void service_ClientConnected(object sender, PoplarCloud.YChannel e)
        {
            //List<NetNode> list= service.NetTree.Root.ClientNode;
            SetList();
            //throw new NotImplementedException();
        }
        private void SetList()
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action(SetList));
                return;
            }
            listView1.Clear();
            foreach (NetNode item in service.NetTree.Root.ClientNode)
            {
                ListViewItem litem = new ListViewItem(item.Id);
                litem.Tag = item;
                listView1.Items.Add(litem);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            service.Start(8889, 500);
            WriteText("开启服务成功");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(listView1.SelectedItems!=null && listView1.SelectedItems.Count>0)
            {
                if (!string.IsNullOrEmpty(textBox1.Text))
                {
                    NetNode item = listView1.SelectedItems[0].Tag as NetNode;

					service.Send(new PoplarCloud.DataPacket.MessagePack(
																item.Id,
																MessageType.Msg,
																Encoding.UTF8.GetBytes(textBox1.Text)));
					//service.Send(service.Find(item.Id), new CustomPack { Text = textBox1.Text });
                }
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            listView1.Clear();
            for (int i = 0; i < 10;i++ )
            {
                ListViewItem litem = new ListViewItem(i.ToString());
                litem.SubItems.Add(i.ToString());
                litem.Tag = i;
                listView1.Items.Add(litem);
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            service.Stop();
        }
    }

}
