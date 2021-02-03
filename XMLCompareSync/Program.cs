using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;

namespace XMLCompareSync
{
    public static class XMLCompSync
    {
        private static XmlDocument document1;
        private static XmlDocument document2;
        public static List<XmlNode> XmlVisFromFile(XmlDocument doc)
        {
            List<XmlNode> nodesCollection = new List<XmlNode>();
            foreach(XmlNode rootNode in doc.ChildNodes)
            {
                nodesCollection.AddRange(XMLVisualisation(rootNode));
            }
            return nodesCollection;
        }
        public static List<XmlNode> XMLVisualisation(XmlNode rootNode)
        {
            List<XmlNode> nodesCollection = new List<XmlNode>();
            if(rootNode.NodeType.ToString()=="Element")
            {
                nodesCollection.Add(rootNode);
                if (rootNode.HasChildNodes)
                {
                    foreach (XmlNode childNode in rootNode.ChildNodes)
                    {
                        nodesCollection.AddRange(XMLVisualisation(childNode));
                    }
                }

            }
            return nodesCollection;
        }
        public static bool IsIdenticalAtrributes(XmlAttributeCollection Attr1, XmlAttributeCollection Attr2)
        {
            if (Attr1 == null || Attr2 == null)
                return Attr1 == Attr2;
            if (Attr1.Count == Attr2.Count)
            {
                foreach (XmlAttribute item1 in Attr1)
                {
                    bool IsFound = false;
                    foreach (XmlAttribute item2 in Attr2)
                    {
                        IsFound = (item1.Name == item2.Name) && (item1.Value == item2.Value);
                    }
                    if (!IsFound)
                    {
                        return IsFound;
                    }
                }
                return true;
            }
            return false;
        }
        public static bool IsProper(XmlNode node1, XmlNode node2)
        {
            if (node1 == null || node2 == null)
                return false;
            if(node1.Name==node2.Name)
            {
                if (IsIdenticalAtrributes(node1.Attributes, node2.Attributes))
                    return true;
            }
            return false;
        }
        public static string FullNameBuilder(XmlNode node)
        {
            string a = "";
            if(node.ParentNode!=null)
            {
                if(node.ParentNode.NodeType.ToString()=="Element")
                     a+=FullNameBuilder(node.ParentNode);
            }
            a += "/"+node.Name;
            return a;
        }
        private static int Search(XmlNode node, List<XmlCompareComponent> list)
        {
            for(int i=0; i<list.Count; i++)
            {
                if (list[i].node1 == node||list[i].node2==node)
                    return i;
            }
            return -1;
        }
        public static List<XmlCompareComponent> NodeStateSetter(XmlNode rootNode1, XmlNode rootNode2)
        {
            List<XmlCompareComponent> stateCollection = new List<XmlCompareComponent>();
            XmlNode needed = rootNode2;
            XmlNode current = rootNode1;
            while (current != null)
            {
                while (needed != null)
                {
                    if (IsProper(current, needed))
                    {
                        XmlCompareComponent component;
                        component.node1 = current;
                        component.node2 = needed;
                        component.state = State.Equal;
                        stateCollection.Add(component);
                        if (current.HasChildNodes && needed.HasChildNodes)
                        {
                            stateCollection.AddRange(NodeStateSetter(current.FirstChild, needed.FirstChild));
                        }
                        else if (!current.HasChildNodes && needed.HasChildNodes || current.HasChildNodes && !needed.HasChildNodes)
                        {
                            component.state = State.Proper;
                            XmlNode parent = current;
                            while (parent != null)
                            {
                                int index = Search(parent, stateCollection);
                                component.node1 = parent;
                                component.node2 = stateCollection[index].node2;

                            }
                        }
                    }
                    needed = needed.NextSibling;
                }
                current = current.NextSibling;
                needed = rootNode2;
            }
            return stateCollection;
        }
        public static List<XmlCompareComponent> AllInOne(XmlDocument doc1, XmlDocument doc2)
        {
            document1 = doc1;
            document2 = doc2;
            List<XmlCompareComponent> statistics = NodeStateSetter(doc1.DocumentElement, doc2.DocumentElement);
            List<XmlNode> nodeList = XmlVisFromFile(doc2);
            foreach(var item in nodeList)
            {
                if(Search(item,statistics)==-1)
                {
                    XmlCompareComponent component;
                    component.node1 = null;
                    component.state = State.Copy;
                    component.node2 = item;
                    statistics.Add(component);
                }
            }
            nodeList = XmlVisFromFile(doc1);
            foreach (var item in nodeList)
            {
                if (Search(item, statistics) == -1)
                {
                    XmlCompareComponent component;
                    component.node1 = item;
                    component.state = State.Copy;
                    component.node2 = null;
                    statistics.Add(component);
                }
            }
            return statistics;
        }
        public static XmlDocument SyncXmlTrees(SyncWay syncWay, List<XmlCompareComponent> XmlComparedList)
        {
            if (syncWay == SyncWay.Unknown)
                return null;
            bool SameRootNodes=false;
            foreach(var item in XmlComparedList)
            {
                if (item.state != State.Copy&&item.state != State.Unknown)
                {
                    SameRootNodes = true;
                    break;
                }
            }
            if(!SameRootNodes)
            {
                return null;
            }
            XmlDocument doc;
            if (SyncWay.FirstIntoSecond == syncWay)
                doc = document2;
            else if (SyncWay.SecondIntoFirst == syncWay)
                doc = document1;
            bool Side;
            Side = SyncWay.FirstIntoSecond == syncWay;
            for (int i = 0; i<XmlComparedList.Count; i++)
            {
                if (XmlComparedList[i].state == State.Copy)
                {
                    if (Side)
                    {
                        if (XmlComparedList[i].node2 == null)
                        {
                            XmlNode current = XmlComparedList[i].node1.ParentNode;
                            XmlNode proper = XmlComparedList[Search(current, XmlComparedList)].node2;
                            
                            proper.AppendChild(document2.ImportNode(XmlComparedList[i].node1,true));
                            List<XmlNode> OnDelete = XMLVisualisation(XmlComparedList[i].node1);
                            foreach (XmlNode node in OnDelete)
                            {
                                XmlComparedList.RemoveAt(Search(node, XmlComparedList));
                            }
                            i = 0;
                        }
                    }
                    else
                    {
                        if (XmlComparedList[i].node1 == null)
                        {
                            XmlNode current = XmlComparedList[i].node2.ParentNode;
                            XmlNode proper = XmlComparedList[Search(current, XmlComparedList)].node1;

                            proper.AppendChild(document1.ImportNode(XmlComparedList[i].node2,true));
                            List<XmlNode> OnDelete = XMLVisualisation(XmlComparedList[i].node2);
                            foreach (XmlNode node in OnDelete)
                            {
                                XmlComparedList.RemoveAt(Search(node, XmlComparedList));
                            }
                            i = 0;
                        }
                    }
                }
            }
            if (Side)
            {
                return document2;
            }
            else
            {
                return document1;
            }
        }
    }
    public enum SyncWay
    {
        Unknown = 0,
        FirstIntoSecond=1,
        SecondIntoFirst=2
    }
    public static class StateTools
    {
        public static string GetSymbol(State state)
        {
            int tmp = (int)state;
            switch(tmp)
            {
                case 1:
                    return "!=";
                case 2:
                    return "==";
                case 3:
                    return "<>";
                default:
                    return "??";
            }
        }
    }
    public enum State
    {
        Unknown = 0,
        Proper = 1,
        Equal = 2,
        Copy = 3
    }
    public struct XmlCompareComponent
    {
        public XmlNode node1;
        public State state;
        public XmlNode node2;
    }


    class Program
    { 
        static void Main(string[] args)
        {
            string firstDoc;
            string secondDoc;
            Console.WriteLine("Введите полный путь первого файла или \"1\", чтобы просмотреть примеры пользования: ");
            firstDoc = Console.ReadLine();
            if(firstDoc=="1")
            {
                firstDoc = "examp3.xml";
                secondDoc = "examp.xml";
            }
            else
            {
                Console.WriteLine("Введите полный путь второго файла: ");
                secondDoc = Console.ReadLine();
            }
            Console.WriteLine("== - узлы равны, != - узлы соотвествуют друг другу, <> - узел отсутсвует в дереве");
            XmlDocument doc = new XmlDocument();
            doc.Load(firstDoc);
            XmlDocument doc2 = new XmlDocument();
            doc2.Load(secondDoc);
            List<XmlCompareComponent> compared = XMLCompSync.AllInOne(doc, doc2);
            Visualisator(compared);
            Console.WriteLine("Выберите способ синхронизации:");
            Console.WriteLine("1-синхронизировать второй файл с первым");
            Console.WriteLine("2-синхронизировать первый файл со вторым");
            Console.WriteLine("P.S: если сравнивались файлы с разными корневыми узлами или\nвведено неправильное число, вернется пустое дерево");
            SyncWay way;
            try
            {
                way = (SyncWay)Int32.Parse(Console.ReadLine());
            }
            catch(Exception)
            {
                way = SyncWay.Unknown;
            }
            XmlDocument resultDocument = XMLCompSync.SyncXmlTrees(way, compared);
            if (way == SyncWay.FirstIntoSecond)
                Visualisator(XMLCompSync.AllInOne(resultDocument, doc));
            else if(way==SyncWay.SecondIntoFirst)
                    Visualisator(XMLCompSync.AllInOne(resultDocument, doc2));
            Console.ReadLine();
            Console.Clear();
            Console.ReadLine();
        }
        private static void Visualisator(List<XmlCompareComponent> statistics)
        {
            foreach (var item in statistics)
            {
                bool IsPrint = false;
                StringBuilder sb = new StringBuilder();
                if (item.node1 != null&&item.node1.NodeType.ToString()=="Element")
                {
                    sb.Append(item.node1.Name);
                    IsPrint = true;
                }
                sb.Append("\t\t\t");
                sb.Append(StateTools.GetSymbol(item.state));
                sb.Append("\t\t\t");
                if (item.node2 != null&&item.node2.NodeType.ToString()=="Element")
                {
                    sb.Append(item.node2.Name);
                    IsPrint = true;
                }
                if(IsPrint)
                    Console.WriteLine(sb.ToString());
            }
        }

    }
}
