using PoplarCloud.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud
{
    public class NetTree
    {
        private NetNode root;
        private Dictionary<string, NetNode> nodesDic = new Dictionary<string, NetNode>();
        public NetNode Root
        {
            get { return root; }
            private set { root = value; }
        }
        public void CreateRoot(string id)
        {
            root = new NetNode(id, "");
            nodesDic.Add(id,root);
        }
        public string FintNextNode(string startId, string endId)
        {
            if (this.root == null)
            {
                return "";
            }
            if (startId == endId)
            {
                return startId;
            }
            if (endId.Equals(this.root.Id))
            {
                return "";
            }
            NetNode node = FindNode(endId);
            if(node.ParentId!=startId)
            {
                return FintNextNode(startId, node.ParentId);
            }
            else
            {
                return node.Id;
            }
        }
        private bool Contains(NetNode node, string id)
        {
            return node.ClientNode.FirstOrDefault(p => p.Id == id) != null;
        }
        public string Serialize()
        {
            if (this.Root != null)
            {
                return JsonConvert.SerializeObject(this.Root);
            }
            else
            {
                return "";
            }
        }
        public bool Contains(string id)
        {
            return nodesDic.ContainsKey(id);
        }
        public bool Remove(string id)
        {
            if(id==this.Root.Id)
            {
                this.root = null;
                this.nodesDic.Clear();
                GC.Collect();
                return true;
            }
            if (Contains(id))
            {
                NetNode node = FindNode(id);
                NetNode pnode = FindNode(node.ParentId);
                if(pnode!=null)
                {
                    pnode.ClientNode.Remove(node);
                }
                RemoveDic(node);
                node.Dispose();
                GC.Collect();
                return true;
            }
            GC.Collect();
            return false;
        }
        private void RemoveDic(NetNode node)
        {
            if(node.ClientNode.Count==0)
            {
                nodesDic.Remove(node.Id);
                return;
            }
            foreach(NetNode n in node.ClientNode)
            {
                RemoveDic(n);
            }
            nodesDic.Remove(node.Id);
        }
        public bool Remove(NetNode node)
        {
            return Remove(node.Id);
        }
        public void Add(NetNode node)
        {
            if (Contains(node.Id))
            {
                throw new Exception("已存在的节点");
            }
            if (string.IsNullOrWhiteSpace(node.ParentId))
            {
                node.ParentId = this.root.Id;
                this.root.ClientNode.Add(node);
                nodesDic.Add(node.Id,node);
            }
            else
            {
                NetNode tnRet = FindNode(node.ParentId);
                if(tnRet==null)
                {
                    throw new Exception("未找到父级节点，添加失败");
                }
                if (tnRet != null)
                {
                    tnRet.ClientNode.Add(node);
                    AddDic(node);
                    //nodesDic.Add(node.Id,node);
                }
            }
        }
        private void AddDic(NetNode node)
        {
            nodesDic.Add(node.Id, node);
            foreach (NetNode n in node.ClientNode)
            {
                AddDic(n);
            }
        }
        public void Add(NetNode parentNode, NetNode node)
        {
            if (Contains(node.Id))
            {
                throw new Exception("已存在的节点");
            }
            node.ParentId = parentNode.Id;
            parentNode.ClientNode.Add(node);
            AddDic(node);
            //nodesDic.Add(node.Id, node);
        }
        public List<NetNode> GetGroup(string group)
        {
           return  this.root.ClientNode.Where(p => p.Group == group).ToList();
        }
        public NetNode FindNode(string id)
        {
            NetNode node=null;
            nodesDic.TryGetValue(id, out node);
            return node;
        }
    }
    public class NetNode:IDisposable
    {
        public NetNode(string id, string parentId)
        {
            this.Id = id;
            this.ParentId = parentId;
            ClientNode = new List<NetNode>();
        }
        [JsonProperty(PropertyName = "N")]
        public List<NetNode> ClientNode { get; internal set; }
        [JsonProperty(PropertyName = "I")]
        public string Id { get; internal set; }
        [JsonProperty(PropertyName = "P")]
        public string ParentId { get; internal set; }
         [JsonProperty(PropertyName = "G")]
        public string Group { get; internal set; }
        public void Dispose()
        {
            ClientNode.Clear();
            ClientNode = null;
            Id = "";
            ParentId = "";
            GC.Collect();
        }
    }
}
 