using System.Collections.Generic;
using System.Xml;

namespace XmlCompareSync
{
    public static class XmlCompareAndSynchronize
    {
        private static XmlDocument document1;
        private static XmlDocument document2;
        public static List<XmlNode> XmlVisFromFile(XmlDocument doc)
        {
            List<XmlNode> nodesCollection = new List<XmlNode>();
            foreach (XmlNode rootNode in doc.ChildNodes)
            {
                nodesCollection.AddRange(XMLVisualisation(rootNode));
            }
            return nodesCollection;
        }
        private static List<XmlNode> XMLVisualisation(XmlNode rootNode)
        {
            List<XmlNode> nodesCollection = new List<XmlNode>();
            if (rootNode.NodeType.ToString() == "Element")
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
        private static bool IsIdenticalAtrributes(XmlAttributeCollection Attr1, XmlAttributeCollection Attr2)
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
        private static bool IsProper(XmlNode node1, XmlNode node2)
        {
            if (node1 == null || node2 == null)
                return false;
            if (node1.Name == node2.Name)
            {
                if (IsIdenticalAtrributes(node1.Attributes, node2.Attributes))
                    return true;
            }
            return false;
        }
        public static string FullNameBuilder(XmlNode node)
        {
            string a = "";
            if (node.ParentNode != null)
            {
                if (node.ParentNode.NodeType.ToString() == "Element")
                    a += FullNameBuilder(node.ParentNode);
            }
            a += "/" + node.Name;
            return a;
        }
        private static int Search(XmlNode node, List<XmlCompareComponent> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].node1 == node || list[i].node2 == node)
                    return i;
            }
            return -1;
        }
        private static List<XmlCompareComponent> NodeStateSetter(XmlNode rootNode1, XmlNode rootNode2)
        {
            List<XmlCompareComponent> stateCollection = new List<XmlCompareComponent>();
            XmlNode needed = rootNode2;
            XmlNode current = rootNode1;
            XmlCompareComponent component;
            while (current != null)
            {
                while (needed != null)
                {
                    if (IsProper(current, needed))
                    {
                        component.node1 = current;
                        component.node2 = needed;
                        component.state = State.Equal;
                        stateCollection.Add(component);
                        if (current.HasChildNodes && needed.HasChildNodes)
                        {
                            stateCollection.AddRange(NodeStateSetter(current.FirstChild, needed.FirstChild));
                        }
                        break;
                    }
                    needed = needed.NextSibling;
                }
                current = current.NextSibling;
                needed = rootNode2;
            }
            return stateCollection;
        }
        public static List<XmlCompareComponent> CompareXmlDocs(XmlDocument doc1, XmlDocument doc2)
        {
            document1 = doc1;
            document2 = doc2;
            List<XmlCompareComponent> statistics = NodeStateSetter(doc1.DocumentElement, doc2.DocumentElement);
            List<XmlNode> nodeList = XmlVisFromFile(doc2);
            foreach (var item in nodeList)
            {
                if (Search(item, statistics) == -1)
                {
                    XmlCompareComponent component;
                    component.node1 = null;
                    component.state = State.Copy;
                    component.node2 = item;
                    statistics.Add(component);
                    component.state = State.Proper;
                    XmlNode parent = item.ParentNode;
                    while (parent != null&&parent.NodeType.ToString()=="Element")
                    {
                        int index = Search(parent, statistics);
                        if (statistics[index].state == State.Copy)
                        {
                            parent = parent.ParentNode;
                            continue;
                        }
                            
                        component.node2 = parent;
                        component.node1 = statistics[index].node1;
                        statistics[index] = component;
                        parent = parent.ParentNode;
                    }

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
                    component.state = State.Proper;
                    XmlNode parent = item.ParentNode;
                    while (parent != null && parent.NodeType.ToString() == "Element")
                    {
                        int index = Search(parent, statistics);
                        if (statistics[index].state == State.Copy)
                        {
                            parent = parent.ParentNode;
                            continue;
                        }

                        component.node1 = parent;
                        component.node2 = statistics[index].node2;
                        statistics[index] = component;
                        parent = parent.ParentNode;
                    }

                }
            }
            return statistics;
        }
        public static XmlDocument SyncXmlTrees(SyncWay syncWay, List<XmlCompareComponent> XmlComparedList)
        {
            if (syncWay == SyncWay.Unknown)
                return null;
            bool SameRootNodes = false;
            foreach (var item in XmlComparedList)
            {
                if (item.state != State.Copy && item.state != State.Unknown)
                {
                    SameRootNodes = true;
                    break;
                }
            }
            if (!SameRootNodes)
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
            for (int i = 0; i < XmlComparedList.Count; i++)
            {
                if (XmlComparedList[i].state == State.Copy)
                {
                    if (Side)
                    {
                        if (XmlComparedList[i].node2 == null)
                        {
                            XmlNode current = XmlComparedList[i].node1.ParentNode;
                            XmlNode proper = XmlComparedList[Search(current, XmlComparedList)].node2;

                            proper.AppendChild(document2.ImportNode(XmlComparedList[i].node1, true));
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

                            proper.AppendChild(document1.ImportNode(XmlComparedList[i].node2, true));
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
}
