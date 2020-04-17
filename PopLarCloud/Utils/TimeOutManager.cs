using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoplarCloud
{
    /// <summary>
    /// 数据超时管理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TimeOutManager<T>
    {
        private string savePath;
        public delegate void DataTimeOutHandle(object sender, List<T> e);
        public event DataTimeOutHandle EndTimeOut;
        protected Dictionary<T, DateTime> dicData = new Dictionary<T, DateTime>();
        /// <summary>
        /// 超时数据
        /// </summary>
        private List<T> timeOutData = new List<T>();
        private Thread timeOutThread;
        /// <summary>
        /// 等待超时数据
        /// </summary>
        public List<T> WaitData
        {
            get
            {
                lock(dicData)
                return dicData.Keys.ToList();
            }
        }
        private int timeOut = 3600;
        /// <summary>
        /// 超时时间（单位秒）
        /// </summary>
        public int TimeOut
        {
            get { return timeOut; }
            set { timeOut = value; }
        }
        ///// <summary>
        ///// 加载时间
        ///// </summary>
        ///// <param name="path"></param>
        //public void LoadData(string path)
        //{
        //    //List<Data> data = SerializationUtils.DeserializeXML<List<Data>>(path);
        //    //foreach (Data pack in data)
        //    //{
        //    //    lock (dicData)
        //    //    this.dicData[pack.Value]= pack.LastTime;
        //    //}
        //}
        /// <summary>
        /// 添加数据，等待超时
        /// </summary>
        /// <param name="data"></param>
        public void AddData(T data)
        {
            Thread.Sleep(10);
            lock(dicData)
            {
                dicData[data] = DateTime.Now;
            }
        }
        public virtual void Remove(T data)
        {
            lock(dicData)
            {
                dicData.Remove(data);
            }
        }
        /// <summary>
        /// 开启超时监测
        /// </summary>
        public void Start()
        {
            if (timeOutThread==null || timeOutThread.IsAlive==false)
            {
                timeOutThread = new Thread(Begin);
                timeOutThread.IsBackground = true;
                timeOutThread.Start();
            }
        }
        private void Begin()
        {
            while(true)
            {
                Thread.Sleep(TimeOut * 1000);
                lock (dicData)
                {
                    List<T> lst = new List<T>();
                    foreach (var item in dicData.Where(item => item.Key != null).ToList())
                    {
                        if (item.Value < DateTime.Now.AddSeconds(-TimeOut))
                        {
                            lst.Add(item.Key);
                            dicData.Remove(item.Key);
                        }
                    }
                    if(EndTimeOut!=null && lst.Count>0)
                    {
                        EndTimeOut(this, lst);
                    }
                }
            }
        }
        /// <summary>
        /// 关闭超时监测
        /// </summary>
        public void Shop()
        {
            timeOutThread.Abort();
            timeOutThread = null;
        }
        public T GetTimeOutData()
        {
            return default(T);
        }
        public class Data
        {
            public T Value { get; set; }
            public DateTime LastTime { get; set; }
        }
    }
}
