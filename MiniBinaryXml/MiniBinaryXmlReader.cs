using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MiniBinaryXml {
    public class MiniBinaryXmlReader {
        public class Node {
            public string Tag;
            public string InnerText;
            public Dictionary<string, string> Attributes = new();
            public List<Node> Children = new();
        }

        public Node Read(Stream stream) {
            using BinaryReader br = new(stream, Encoding.UTF8, leaveOpen: true);
            byte[] magic = br.ReadBytes(4);
            if (Encoding.ASCII.GetString(magic) != "MBXM") throw new Exception("Invalid format");
            byte version = br.ReadByte();
            br.ReadBytes(3); // reserved

            // String pool
            int strCount = br.ReadInt32();
            int[] offsets = new int[strCount];
            for (int i = 0; i < strCount; i++) offsets[i] = br.ReadInt32();
            long strBase = stream.Position;
            List<string> stringPool = new();
            for (int i = 0; i < strCount; i++) {
                stream.Position = strBase + offsets[i];
                ushort len = br.ReadUInt16();
                byte[] bytes = br.ReadBytes(len);
                stringPool.Add(Encoding.UTF8.GetString(bytes));
            }
            stream.Position = strBase + offsets[^1];
            ushort lastLen = br.ReadUInt16();
            br.ReadBytes(lastLen); // skip last string content

            // Nodes
            stream.Position = strBase + offsets[^1] + 2 + lastLen;
            int nodeCount = br.ReadInt32();
            var rawNodes = new List<(int tag, int innerText, int parent, List<(int, int)> attrs, List<int> children)>();
            for (int i = 0; i < nodeCount; i++) {
                int tag = br.ReadInt32();
                int innerText = br.ReadInt32();
                int parent = br.ReadInt32();
                ushort attrCount = br.ReadUInt16();
                ushort childCount = br.ReadUInt16();
                var attrs = new List<(int, int)>();
                var children = new List<int>();
                for (int j = 0; j < attrCount; j++) attrs.Add((br.ReadInt32(), br.ReadInt32()));
                for (int j = 0; j < childCount; j++) children.Add(br.ReadInt32());
                rawNodes.Add((tag, innerText, parent, attrs, children));
            }

            // Rebuild
            Node[] nodes = new Node[nodeCount];
            for (int i = 0; i < nodeCount; i++) {
                var n = new Node { Tag = stringPool[rawNodes[i].tag] };
                if (rawNodes[i].innerText >= 0) n.InnerText = stringPool[rawNodes[i].innerText];
                foreach (var (nk, nv) in rawNodes[i].attrs) n.Attributes[stringPool[nk]] = stringPool[nv];
                nodes[i] = n;
            }
            for (int i = 0; i < nodeCount; i++) {
                foreach (var ci in rawNodes[i].children) nodes[i].Children.Add(nodes[ci]);
            }
            for (int i = 0; i < nodeCount; i++)
                if (rawNodes[i].parent == -1)
                    return nodes[i];
            throw new Exception("No root node found");
        }

        public XElement ReadAsXml(Stream stream) {
            Node root = Read(stream);
            return BuildXml(root);
        }

        private XElement BuildXml(Node node) {
            var elem = new XElement(XName.Get(node.Tag));
            if (!string.IsNullOrEmpty(node.InnerText)) SetDirectText(elem, node.InnerText);
            foreach (var attr in node.Attributes) elem.SetAttributeValue(XName.Get(attr.Key), attr.Value);
            foreach (var child in node.Children) elem.Add(BuildXml(child));
            return elem;
        }

        void SetDirectText(XElement element, string newText) {
            // 删除所有直接子级中的 XText 节点
            var textNodes = element.Nodes().OfType<XText>().ToList(); // 注意：先 ToList 防止修改集合时遍历出错
            foreach (var node in textNodes) node.Remove();

            // 插入新的文本节点，放在最前面
            element.AddFirst(new XText(newText));
        }
    }
}