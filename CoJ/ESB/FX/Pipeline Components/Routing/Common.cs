namespace CoJ.ESB.FX.PIP.ASM.Routing
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Diagnostics;
    using System.Xml;

    using Microsoft.BizTalk.Message.Interop;
    using Microsoft.BizTalk.Component.Interop;

    using CoJ.ESB.FX;
    #endregion

    class Common
    {
        #region Enumerators
        public enum Direction
        {
            Inbound = 0,
            Outbound = 1
        }
        #endregion

        #region Constants
        private const string sLOG_PREFIX = "LOG";
        #endregion

        #region Common pipeline component methods.
        public static void WriteDocuments(Stream strm, IPipelineContext pContext, string docType, string txNo, string BTSID, IBaseMessage pInMsg, string fileExtension,
                                          bool isFlatFile, Direction direction = Direction.Inbound)
        {
            bool bPathExists = false;
            string sPathAndFile = string.Empty;
            string sFilePath = string.Empty;
            string sRootPath = string.Empty;
            string sDate = string.Empty;
            string contextPropertyXml = string.Empty;

            string InOutBound = (direction == Direction.Inbound ? @"\Inbound" : @"\OutBound");

            try
            {
                if (string.IsNullOrEmpty(fileExtension))
                {
                    fileExtension = ".xml";
                }

                if (!fileExtension.Contains("."))
                {
                    fileExtension = "." + fileExtension;
                }

                string fileDocType = docType.Replace("\\", "").Replace("/", "^");

                if (String.IsNullOrEmpty(sRootPath))
                {
                    Common.SetupLogDocsPath(ref bPathExists, ref sFilePath,ref sRootPath,ref sDate);
                }

                sFilePath = sRootPath + sDate + InOutBound + "\\" + fileDocType.Replace("^", "");

                bPathExists = System.IO.Directory.Exists(sFilePath);

                if (bPathExists == false)
                {
                    CreateFolder(sFilePath);
                }

                if (txNo != "Unknown" || docType == "ERROR")
                {
                    if(fileDocType.ToLower().Equals(txNo.ToLower()))
                    {
                        sPathAndFile = sFilePath + "\\" + sLOG_PREFIX + "_" + fileDocType + "_" + BTSID + fileExtension;
                    }
                    else
                    {
                        if (isFlatFile)
                        {
                            sPathAndFile = sFilePath + "\\" + sLOG_PREFIX + "_" + txNo + "_" + BTSID + fileExtension;
                        }
                        else
                        {
                            sPathAndFile = sFilePath + "\\" + sLOG_PREFIX + "_" + fileDocType + "_" + txNo + "_" + BTSID + fileExtension;
                        }
                    }

                    if (direction == Direction.Inbound)
                    {
                        if(System.IO.File.Exists(sFilePath))
                        {
                            string postFix = "(" + System.DateTime.Now.ToString("yyyyMMddhhmmss") + ")";


                            sPathAndFile = sPathAndFile.Replace(".xml", postFix + "." + fileExtension);
                        }

                        using (FileStream logDocsWriter = new FileStream(sPathAndFile, FileMode.Create, FileAccess.Write))
                        {
                            lock (logDocsWriter)
                            {
                                ReadWriteStream(strm, logDocsWriter);
                            }
                        }
                    }
                    else
                    {
                        string message = string.Empty;

                        StreamReader reader = null;

                        if (strm != null)
                        {
                            strm.Position = 0;

                            reader = new StreamReader(strm);
                            message = reader.ReadToEnd();
                        }

                        if (!string.IsNullOrEmpty(message))
                        {
                            if (pInMsg != null)
                            {
                                using (StringWriter xmlString = new StringWriter())
                                {
                                    using (XmlTextWriter writer = new XmlTextWriter(xmlString))
                                    {
                                        writer.WriteStartDocument();
                                        writer.WriteStartElement("ResubmitMessage", "http://coj.esb/process/resubmit");
                                        writer.WriteAttributeString("version", "1.0.0");
                                        writer.WriteStartElement("Message");

                                        writer.WriteStartElement("Part");
                                        writer.WriteString("<![CDATA[" + message + "]]>");
                                        writer.WriteEndElement();

                                        writer.WriteStartElement("ContextProperties");

                                        int contextLength = (int)pInMsg.Context.CountProperties;

                                        for (int i = 0; i <= contextLength - 1; i++)
                                        {
                                            string name = string.Empty;
                                            string nameSpace = string.Empty;
                                            string value = string.Empty;

                                            pInMsg.Context.ReadAt(i, out name, out nameSpace);
                                            value = pInMsg.Context.Read(name, nameSpace).ToString();

                                            writer.WriteStartElement("ContextProperty");
                                            writer.WriteAttributeString("name", name);
                                            writer.WriteAttributeString("type", nameSpace);
                                            writer.WriteAttributeString("value", value);
                                            writer.WriteEndElement();
                                        }

                                        writer.WriteEndElement();

                                        writer.WriteEndElement();

                                        writer.WriteEndElement();
                                        writer.WriteEndDocument();
                                    }

                                    contextPropertyXml = xmlString.ToString();
                                }

                                if (!string.IsNullOrEmpty(contextPropertyXml))
                                {
                                    lock (contextPropertyXml)
                                    {
                                        if (File.Exists(sPathAndFile))
                                        {
                                            string postFix = "(" + System.DateTime.Now.ToString("yyyyMMddhhmmss") + ")";

                                            sPathAndFile = sPathAndFile.Replace(".xml", postFix + ".xml");
                                        }

                                        XmlDocument contextPropertyDocument = new XmlDocument();
                                        contextPropertyDocument.LoadXml(contextPropertyXml);
                                        contextPropertyDocument.Save(sPathAndFile);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("space on the disk"))
                {
                    if (!string.IsNullOrEmpty(sPathAndFile))
                    {
                        if (System.IO.File.Exists(sPathAndFile))
                        {
                            try
                            {
                                System.IO.File.Delete(sPathAndFile);
                            }
                            catch { }
                        }
                    }

                    throw new Exception("Message not committed to disk. Disk full error");
                }
                else
                {
                    if (e.Message.Contains("An item with the same key has already been added"))
                    {
                        //NULL
                    }
                    else
                    {
                        if (!e.Message.Contains("Could not find a part"))
                        {
                            throw e;
                        }
                    }
                }
            }
        }

        public static void WriteEventTracking(string trackValue)
        {
            if (!EventLog.SourceExists("PipelineTracking"))
            {
                EventSourceCreationData source = new EventSourceCreationData("PipelineTracking", "Application");
                EventLog.CreateEventSource(source);
            }

            EventLog oEventLog = new EventLog();
            oEventLog.Source = "PipelineTracking";
            oEventLog.WriteEntry(trackValue);
        }

        public static void GetTransactionInfo(XmlDocument oXmlDocument, out string osDocName, out string isTrxNo, XmlDocument configXML, string documentType)
        {
            string TxElement = string.Empty;
            isTrxNo = string.Empty;

            try
            {
                osDocName = documentType.Replace("\\", "").Replace("/", "");

                XmlNodeList configXmlNodeList = configXML.SelectNodes("//TransactionElements/TransactionValue");

                for (int i = 0; i < configXmlNodeList.Count; ++i)
                {
                    TxElement = configXmlNodeList.Item(i).InnerText.Replace("%NODENAME%", osDocName).ToString();
                    TxElement = TxElement.Remove(0, 2);
                    if (TxElement.IndexOf("@") > -1)
                        TxElement = TxElement.Replace("/@", "']@@*[local-name()='");
                    TxElement = TxElement.Replace("/", "']/*[local-name()='");
                    TxElement = "/*[local-name()='" + TxElement;
                    TxElement = TxElement + "']";

                    if (TxElement.IndexOf("@") > -1)
                        TxElement = TxElement.Replace("@@", "/@");

                    if (FindElement(oXmlDocument, TxElement, out isTrxNo))
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (isTrxNo.Trim() == string.Empty)
                {
                    isTrxNo = "Unknown";
                }
            }
        }

        public static bool FindElement(XmlDocument oXmlDocument, string NodePath, out string oxTrxNo)
        {
            bool RetValue = false;
            oxTrxNo = string.Empty;

            try
            {
                XmlNode oXmlNode = oXmlDocument.SelectSingleNode(NodePath);

                if (oXmlNode != null)
                {
                    oxTrxNo = oXmlNode.InnerText;
                    RetValue = true;
                }
            }
            catch (XmlException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return RetValue;
        }

        public static void CreateFolder(object path)
        {
            string folder = (string)path;
            System.IO.Directory.CreateDirectory(folder);
        }

        private static bool IsDataXml(Stream stream)
        {
            bool isDataXml = true;

            try
            {
                XmlDocument dataValidator = new XmlDocument();
                dataValidator.Load(stream);
            }
            catch
            {
                isDataXml = false;
            }

            return isDataXml;
        }

        public static void ReadWriteStream(Stream source, Stream destination)
        {
            int Length = 256;
            Byte[] buffer = new Byte[Length];
            int bytesRead = source.Read(buffer, 0, Length);

            while (bytesRead > 0)
            {
                destination.Write(buffer, 0, bytesRead);
                bytesRead = source.Read(buffer, 0, Length);
            }

            destination.Close();

            // Make sure that the source stream is at position 0, 
            // else the message that will be passed on will be empty.
            source.Position = 0;   
        }

        public static void ExceptionFileToDisk(Stream sourceStream, string sPathAndFile)
        {
            if (sourceStream.Length > 0)
            {
                try
                {
                    System.IO.FileStream oFileStream = new System.IO.FileStream(sPathAndFile, System.IO.FileMode.Create, System.IO.FileAccess.Write,
                                                                                FileShare.Write);
                    sourceStream.Position = 0;
                    int numBytesToRead = (int)sourceStream.Length;
                    byte[] byteDoc = new byte[numBytesToRead];
                    int numBytesRead = 0;
                    int n = sourceStream.Read(byteDoc, numBytesRead, numBytesToRead);
                    oFileStream.Write(byteDoc, 0, byteDoc.Length);
                    oFileStream.Flush();
                }
                catch { }
            }
        }

        public static void ManipulateExceptionPath(string documentType, string txNo, string TrackingId, ref bool bPathExists, ref string sPathAndFile,
                                                   ref string sFilePath, string sRootPath, string sDate, string InOutBound)
        {
            string fileDocType = documentType.Replace("\\", "").Replace("/", "^");

            sFilePath = sRootPath + sDate + InOutBound + "\\" + fileDocType.Replace("^", "");

            bPathExists = System.IO.Directory.Exists(sFilePath);

            if (bPathExists == false)
            {
                CreateFolder(sFilePath);
            }

            sPathAndFile = sFilePath + "\\" + sLOG_PREFIX + "_" + fileDocType + "_" + txNo + "_" + TrackingId + ".xml";

            if (File.Exists(sPathAndFile))
            {
                sPathAndFile = sPathAndFile.Insert(sPathAndFile.Length - 4, "_" + Math.Abs(Guid.NewGuid().GetHashCode()).ToString());
            }
        }

        public static void ReadExceptionContextProperties(IBaseMessage pInMsg, ref string documentType, ref string txNo, ref string TrackingId)
        {
            txNo = ContextProperties.Get(pInMsg, "TxNum", "Send");
            documentType = ContextProperties.Get(pInMsg, "DocumentType", "Send");
            TrackingId = ContextProperties.Get(pInMsg, "TrackingId", "Unknown");
        }

        public static void SetupLogDocsPath(ref bool bPathExists, ref string sFilePath, ref string sRootPath, ref string sDate)
        {
                string xmlconfig = string.Empty;

                if (Cache.ContainsKey("BizTalk_LogDocs"))
                {
                    xmlconfig = Cache.Get("BizTalk_LogDocs").ToString();
                }
                else
                {
                    xmlconfig = Cache.GetConfigSettingFromWMI("BizTalk", "LogDocs");
                }

                XmlDocument oConfigXML = new XmlDocument();
                oConfigXML.LoadXml(xmlconfig);
                sRootPath = string.Empty;

                sRootPath = oConfigXML.SelectSingleNode("//TransactionElements/@LoggedDocsPath").InnerText;

                sDate = System.DateTime.Now.ToString("yyyyMMMdd");

                sFilePath = sRootPath + sDate;
                bPathExists = System.IO.Directory.Exists(sFilePath);

                if (bPathExists == false)
                {
                    CreateFolder(sFilePath);
                }
        }

        public static IBaseMessagePart CreateIBaseMsgPart(IBaseMessage pInMsg, ref long position, ref Stream sourceStream)
        {
            if (!sourceStream.CanSeek)
            {
                SeekableReadOnlyStream seekableStream = new SeekableReadOnlyStream(sourceStream);

                pInMsg.BodyPart.Data = seekableStream;

                sourceStream = pInMsg.BodyPart.Data;
            }

            position = sourceStream.Position;

            sourceStream.Position = position;

            IBaseMessagePart bodyPart = pInMsg.BodyPart;

            return bodyPart;
        }

        public static Stream CreateSourceStream(IBaseMessage pInMsg)
        {
            SeekableReadOnlyStream stream = new SeekableReadOnlyStream(pInMsg.BodyPart.GetOriginalDataStream());
            Stream sourceStream = pInMsg.BodyPart.GetOriginalDataStream();

            return sourceStream;
        }

        public static void ValidateStream(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            if (null == pContext)
            {
                throw new ArgumentNullException("pContext");
            }

            if (null == pInMsg)
            {
                throw new ArgumentNullException("pInMsg");
            }

            if (null == pInMsg.BodyPart || null == pInMsg.BodyPart.GetOriginalDataStream())
            {
                throw new ArgumentNullException("pInMsg");
            }
        }
        #endregion
    }
}
