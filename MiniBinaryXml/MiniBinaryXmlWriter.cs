using System.Text;
using System.Xml.Linq;

namespace MiniBinaryXml {
    public class MiniBinaryXmlWriter {
        private List<string> stringPool = new();
        private Dictionary<string, int> stringIndex = new();
        private List<Node> nodes = new();

        private int AddString(string s) {
            if (stringIndex.TryGetValue(s, out int idx)) return idx;
            idx = stringPool.Count;
            stringPool.Add(s);
            stringIndex[s] = idx;
            return idx;
        }

        class Node {
            public int TagNameIdx;
            public int InnerTextIdx;//xml节点的值，若小于0则表示为空
            public int ParentIdx;
            public List<(int nameIdx, int valueIdx)> Attributes = new();
            public List<int> Children = new();
        }

        public void Write(XElement doc, Stream stream) {
            nodes.Clear();
            stringPool.Clear();
            stringIndex.Clear();

            Traverse(doc, -1);

            using BinaryWriter bw = new(stream, Encoding.UTF8, leaveOpen: true);

            // Header
            bw.Write(Encoding.ASCII.GetBytes("MBXM"));
            bw.Write((byte)1); // version
            bw.Write(new byte[3]); // reserved

            // String Pool
            bw.Write(stringPool.Count);
            using MemoryStream strData = new();
            using BinaryWriter strWriter = new(strData);
            List<int> offsets = new();
            int offset = 0;
            foreach (var s in stringPool) {
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                offsets.Add(offset);
                strWriter.Write((ushort)bytes.Length);
                strWriter.Write(bytes);
                offset += 2 + bytes.Length;
            }
            foreach (var o in offsets) bw.Write(o);
            bw.Write(strData.ToArray());

            // Nodes
            bw.Write(nodes.Count);
            foreach (var node in nodes) {
                bw.Write(node.TagNameIdx);
                bw.Write(node.InnerTextIdx);
                bw.Write(node.ParentIdx);
                bw.Write((ushort)node.Attributes.Count);
                bw.Write((ushort)node.Children.Count);
                foreach (var (nameIdx, valIdx) in node.Attributes) {
                    bw.Write(nameIdx);
                    bw.Write(valIdx);
                }
                foreach (var childIdx in node.Children) {
                    bw.Write(childIdx);
                }
            }
        }

        private int Traverse(XElement elem, int parentIdx) {
            var node = new Node { TagNameIdx = AddString(elem.Name.ToString()), ParentIdx = parentIdx };
            foreach (XAttribute attr in elem.Attributes()) {
                node.Attributes.Add((AddString(attr.Name.ToString()), AddString(attr.Value)));
            }
            node.InnerTextIdx = string.IsNullOrEmpty(GetDirectText(elem)) ? -1 : AddString(GetDirectText(elem));
            int myIdx = nodes.Count;
            nodes.Add(node);

            foreach (XElement child in elem.Elements()) {
                int childIdx = Traverse(child, myIdx);
                node.Children.Add(childIdx);
            }
            return myIdx;
        }

        string GetDirectText(XElement element) {
            return string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value));
        }
    }
}