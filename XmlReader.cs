using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace ZScannerRecovery
{
    static class XmlReader
    {
        public static string GetScannerIdFromXml(string xml, string model)
        {
            string scannerID = String.Empty;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList nodeList = doc.GetElementsByTagName("scanner");
            string currentID, currentModel;

            try
            {
                foreach (XmlNode node in nodeList)
                {
                    currentID = String.Empty;
                    currentModel = String.Empty;

                    for (int i = 0; i < node.ChildNodes.Count; i++)
                    {
                        if (node.ChildNodes[i].Name == "scannerID")
                        {
                            currentID = node.ChildNodes[i].InnerText;
                        }
                        else if (node.ChildNodes[i].Name == "modelnumber")
                        {
                            currentModel = node.ChildNodes[i].InnerText;
                        }
                    }
                    if (currentModel.StartsWith(model))
                    {
                        scannerID = currentID;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }

           return scannerID;
        }
    }
}
