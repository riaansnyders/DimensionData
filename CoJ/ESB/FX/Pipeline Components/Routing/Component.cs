namespace CoJ.ESB.FX.PIP.ASM.Routing
{
    #region Using Directives
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    using Microsoft.BizTalk.Bam.EventObservation;
    using Microsoft.BizTalk.Component;
    using Microsoft.BizTalk.Component.Interop;
    using Microsoft.BizTalk.Message.Interop;

    using Microsoft.Practices.ESB.ExceptionHandling;
    using Microsoft.Practices.ESB.ExceptionHandling.Schemas.Faults;
    using Microsoft.Practices.ESB.ExceptionHandling.Schemas.Property;

    using Microsoft.RuleEngine;

    using Microsoft.XLANGs.BaseTypes;

    using CoJ.ESB.FX;
    using CoJ.ESB.FX.ITIN;
    #endregion

    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    [ComponentCategory(CategoryTypes.CATID_Validate)]
    [System.Runtime.InteropServices.Guid("15D25754-01DB-4fee-A281-A70CEA2D09F2")]

    public class Component :
    BaseCustomTypeDescriptor,
    IBaseComponent,
    Microsoft.BizTalk.Component.Interop.IPersistPropertyBag,
    IDisassemblerComponent,
    IComponentUI
    {
        private static string defaultContextNamespace = "http://CoJ.FX/ContextProperties";

        private bool IsFlatFile = false;

        private XmlDasmComp disassembler = new XmlDasmComp();
        static ResourceManager resManager = new ResourceManager("CoJ.ESB.FX.Pipelines", Assembly.GetExecutingAssembly());
        private static Queue outputMessages = new Queue();

        #region Constructor
        public Component()
            :
            base(resManager)
        {
            disassembler.AllowUnrecognizedMessage = true;
        }
        #endregion

        #region IBaseComponent
        /// <summary>
        /// Name of the component.
        /// </summary>
        [Browsable(false)]
        public string Name
        {
            get { return "CoJ.ESB.FX.PIP.ASM.Routing.Component"; }
        }

        /// <summary>
        /// Version of the component.
        /// </summary>
        [Browsable(false)]
        public string Version
        {
            get { return "2.0.10.0"; }
        }

        /// <summary>
        /// Description of the component.
        /// </summary>
        [Browsable(false)]
        public string Description
        {
            get { return "CoJ.ESB.FX.PIP.ASM.Routing"; }
        }

        #endregion

        #region IComponent
        public void Disassemble(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            string documentType = string.Empty;
            string nodeName = string.Empty;
            string defaultContextNamespace = string.Empty;
            string pipelineActivityId = string.Empty;
            string sendActivityId = string.Empty;
            string activityId = string.Empty;
            string txNo = string.Empty;
            string TrackingId = string.Empty;
            string originalFileExtenstion = string.Empty;
            string interChangeId = string.Empty;
            string originalFileName = string.Empty;

            long position = 0;

            try
            {
                lock (outputMessages)
                {
                    interChangeId = (string)pInMsg.Context.Read("InterchangeID", "http://schemas.microsoft.com/BizTalk/2003/system-properties");
                    interChangeId = interChangeId.Replace("{", "").Replace("}", "").ToLower();

                    #region / * Internal Pipeline Gunk (Not refactorable)*/
                    if (null == pContext)
                        throw new ArgumentNullException("pContext");

                    if (null == pInMsg)
                        throw new ArgumentNullException("pInMsg");

                    if (null == pInMsg.BodyPart || null == pInMsg.BodyPart.GetOriginalDataStream())
                        throw new ArgumentNullException("pInMsg");

                    SeekableReadOnlyStream stream = new SeekableReadOnlyStream(pInMsg.BodyPart.GetOriginalDataStream());
                    Stream sourceStream = pInMsg.BodyPart.GetOriginalDataStream();

                    // Check if source stream can seek
                    if (!sourceStream.CanSeek)
                    {
                        // Create a virtual (seekable) stream
                        SeekableReadOnlyStream seekableStream = new SeekableReadOnlyStream(sourceStream);

                        // Set new stream for the body part data of the input message. This new stream will then used for further processing.
                        // We need to do this because input stream may not support seeking, so we wrap it with a seekable stream.
                        pInMsg.BodyPart.Data = seekableStream;

                        // Replace sourceStream with a new seekable stream wrapper
                        sourceStream = pInMsg.BodyPart.Data;
                    }

                    // Preserve the stream position
                    position = sourceStream.Position;
                    #endregion

                    pInMsg.BodyPart.Data.Position = 0;

                    XmlDocument inDocument = new XmlDocument();
                    string sourceString = string.Empty;

                    string fileName = string.Empty;

                    fileName = GetReceivedFileName(pInMsg, fileName);

                    originalFileExtenstion = fileName.Split(".".ToCharArray())[1].ToString();

                    PromoteProperties(pInMsg, ref documentType, nodeName, ref stream, ref sourceStream, inDocument, ref sourceString, IsFlatFile, fileName);

                    WriteMessageToLogDocs(pContext, pInMsg, ref documentType, ref txNo, ref TrackingId, ref position, ref sourceStream, originalFileExtenstion);

                    if (!IsFlatFile)
                    {
                        originalFileName = fileName;

                        pInMsg.BodyPart.Data.Position = 0;
                        inDocument.Load(pInMsg.BodyPart.Data);

                        PromoteBasePropertiesForXml(pInMsg, documentType, txNo, inDocument);

                        RouteMessageViaItinerary(pContext, pInMsg, documentType, originalFileName, interChangeId);
                    }
                    else
                    {
                        originalFileName = string.Empty;

                        originalFileName = FilterContextPropertiesForFileLabel(pInMsg, originalFileName);

                        originalFileName = GetTransactionNumberForFlatFile(originalFileName);

                        string configurationState = GetConfigurationCultureForFlatFile(originalFileName);

                        PromoteBasePropertiesForFlatFile(pInMsg, documentType, originalFileName);

                        RouteFlatMessageViaItinerary(pContext, pInMsg, documentType, string.Empty, originalFileName, interChangeId, 
                                                     configurationState);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string GetReceivedFileName(IBaseMessage pInMsg, string fileName)
        {
            try
            {
                fileName = pInMsg.Context.Read("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties").ToString();
            }
            catch
            {
                try
                {
                    fileName = pInMsg.Context.Read("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties").ToString();
                }
                catch
                {
                    fileName = pInMsg.Context.Read("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties").ToString();
                }
            }

            return fileName;
        }

        private void PromoteProperties(IBaseMessage pInMsg, ref string documentType, string nodeName, ref SeekableReadOnlyStream stream, 
                                       ref Stream sourceStream, XmlDocument inDocument, ref string sourceString, bool isFlatFile, 
                                       string originalFileName)
        {
            XmlDocument receivedDocument = ValidateXml(sourceStream);

            GetDocumentFormatAndType(pInMsg, ref documentType, ref stream, ref sourceStream, inDocument,
                                     ref sourceString, receivedDocument);

            string txNum = GetDocumentTransactionNumber(documentType, nodeName, receivedDocument);

            PromoteContextProperties(pInMsg, documentType, txNum, IsFlatFile,originalFileName);

            stream.Position = 0;
            pInMsg.BodyPart.Data = stream;
        }

        private void WriteMessageToLogDocs(IPipelineContext pContext, IBaseMessage pInMsg, ref string documentType, 
                                           ref string txNo, ref string TrackingId, ref long position, ref Stream sourceStream, 
                                           string fileExtension)
        {
            IBaseMessagePart bodyPart = Common.CreateIBaseMsgPart(pInMsg, ref position, ref sourceStream);

            try
            {
                bodyPart = Common.CreateIBaseMsgPart(pInMsg, ref position, ref sourceStream);

                if (bodyPart != null)
                {
                    ReadContextProperties(pContext, pInMsg, ref documentType, ref txNo, ref TrackingId);

                    WriteDocuments(sourceStream, pContext, documentType, txNo, TrackingId, pInMsg, fileExtension);

                    bodyPart.Data = sourceStream;
                    pContext.ResourceTracker.AddResource(sourceStream);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                sourceStream.Position = position;
            }
        }

        private static void PromoteBasePropertiesForXml(IBaseMessage pInMsg, string documentType, string txNo, XmlDocument inDocument)
        {
            pInMsg.Context.Write("DocumentType", "http://CoJ.FX/ContextProperties", documentType);
            pInMsg.Context.Promote("DocumentType", "http://CoJ.FX/ContextProperties", documentType);

            pInMsg.Context.Write("NodeName", "http://CoJ.FX/ContextProperties", documentType);
            pInMsg.Context.Promote("NodeName", "http://CoJ.FX/ContextProperties", documentType);

            txNo = ReturnTransaction(inDocument, documentType);

            pInMsg.Context.Write("TxNum", "http://CoJ.FX/ContextProperties", txNo);
            pInMsg.Context.Promote("TxNum", "http://CoJ.FX/ContextProperties", txNo);
        }

        private static void PromoteBasePropertiesForFlatFile(IBaseMessage pInMsg, string documentType, string originalFileName)
        {
            pInMsg.Context.Write("TxNum", "http://CoJ.FX/ContextProperties", originalFileName);
            pInMsg.Context.Promote("TxNum", "http://CoJ.FX/ContextProperties", originalFileName);

            pInMsg.Context.Write("DocumentType", "http://CoJ.FX/ContextProperties", documentType);
            pInMsg.Context.Promote("DocumentType", "http://CoJ.FX/ContextProperties", documentType);

            pInMsg.Context.Write("NodeName", "http://CoJ.FX/ContextProperties", documentType);
            pInMsg.Context.Promote("NodeName", "http://CoJ.FX/ContextProperties", documentType);
        }

        private void RouteMessageViaItinerary(IPipelineContext pContext, IBaseMessage pInMsg, string documentType,
                                              string originalFileName, string interChangeId)
        {
            string documentRootNode = string.Empty;
            string itineraryType = string.Empty;
            string configCulture = string.Empty;

            Stream messageStream = pInMsg.BodyPart.Data;
            messageStream.Position = 0;

            IBaseMessage originalIBase = CreateNewReturnMessage(pInMsg, pContext, messageStream);

            XmlDocument document = ValidateXml(messageStream);

            documentRootNode = GetItinaryNameFromXmlBasedDocuments(documentRootNode, document);
            itineraryType = GetItinaryTypeFromXmlBasedDocuments(document);
            configCulture = GetConfigCultureFromXmlBasedDocuments(configCulture, document);

            string itineraryXml = Cache.GetItinerarySettingFromWMI("Itinerary", documentType);

            if (!string.IsNullOrEmpty(itineraryXml))
            {
                XmlDocument itineraryDocument = new XmlDocument();
                itineraryDocument.LoadXml(itineraryXml);

                MessagingBasedRouting(pContext, pInMsg, documentRootNode, itineraryDocument, documentType, itineraryType,
                                      originalFileName, interChangeId, configCulture);

                ProcessBasedRouting(pContext, originalIBase, documentRootNode, itineraryDocument, itineraryType,
                                    documentRootNode, originalFileName, interChangeId, configCulture);
            }
            else
            {
                throw new ApplicationException("Itinerary not found! Message could not be routed!");
            }
        }

        private static string GetItinaryNameFromXmlBasedDocuments(string documentRootNode, XmlDocument document)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                stream.Position = 0;

                document.Save(stream);

                stream.Position = 0;

                XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings());

                while (reader.Read())
                {
                    if (reader.LocalName == "Origin")
                    {
                        documentRootNode = reader.ReadString();

                        break;
                    }
                }
            }
            catch
            {
                documentRootNode = document.DocumentElement.LocalName;
            }

            if (string.IsNullOrEmpty(documentRootNode))
            {
                documentRootNode = document.DocumentElement.LocalName;
            }

            return documentRootNode;
        }

        private static string GetItinaryTypeFromXmlBasedDocuments(XmlDocument document)
        {
            string itineraryType = string.Empty;

            try
            {
                MemoryStream stream = new MemoryStream();
                stream.Position = 0;

                document.Save(stream);

                stream.Position = 0;

                XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings());

                while (reader.Read())
                {
                    if (reader.LocalName == "OriginType")
                    {
                       itineraryType = reader.ReadString();

                        break;
                    }
                }
            }
            catch
            {
                itineraryType = string.Empty;
            }

            return itineraryType;
        }

        private static string GetConfigCultureFromXmlBasedDocuments(string configCulture, XmlDocument document)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                stream.Position = 0;

                document.Save(stream);

                stream.Position = 0;

                XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings());

                while (reader.Read())
                {
                    if (reader.LocalName == "Route")
                    {
                        configCulture = reader.ReadString();

                        break;
                    }
                }
            }
            catch
            {
                configCulture = document.DocumentElement.LocalName;
            }

            if (string.IsNullOrEmpty(configCulture))
            {
                configCulture = document.DocumentElement.LocalName;
            }

            return configCulture;
        }

        private void RouteFlatMessageViaItinerary(IPipelineContext pContext, IBaseMessage pInMsg, string documentType, 
                                                  string itineraryType, string originalFileName, string interChangeId, 
                                                  string configurationState)
        {
            MemoryStream messageStream = new MemoryStream();
            messageStream.Position = 0;

            pInMsg.BodyPart.Data.Position = 0;
            pInMsg.BodyPart.Data.CopyTo(messageStream);

            messageStream.Position = 0;

            IBaseMessage originalIBase = CreateNewReturnMessage(pInMsg, pContext, messageStream);

            string itineraryXml = Cache.GetItinerarySettingFromWMI("Itinerary", documentType);

            if (!string.IsNullOrEmpty(itineraryXml))
            {
                XmlDocument itineraryDocument = new XmlDocument();
                itineraryDocument.LoadXml(itineraryXml);

                MessagingBasedRouting(pContext, pInMsg, documentType, itineraryDocument, documentType, itineraryType,
                                      originalFileName, interChangeId, configurationState);

                ProcessBasedRouting(pContext, originalIBase, documentType, itineraryDocument,
                                    itineraryType, documentType, originalFileName, interChangeId, configurationState);
            }
            else
            {
                throw new ApplicationException("Itinerary not found! Message could not be routed!");
            }
        }
        #endregion

        #region IComponent GetNext
        public IBaseMessage GetNext(IPipelineContext pContext)
        {
           if (outputMessages.Count > 0)
           {
              return (IBaseMessage)outputMessages.Dequeue();
           }
           else
           {
              return null;
           }
        }
        #endregion

        #region Promote Properties Methods
        private string Retrieve_Namespace(string nodeValue)
        {
            string nameSpace = string.Empty;

            nameSpace = Cache.GetConfigSettingFromWMI("DocumentType", nodeValue + "_Namespace");

            return nameSpace;
        }

        private static string ReturnTransaction(XmlDocument inDocument, string nodeName)
        {
            string TxElement = string.Empty;
            string isTrxNo = string.Empty;

            try
            {
                string config = Cache.GetConfigSettingFromWMI("BizTalk", "LogDocs");

                XmlDocument configXML = new XmlDocument();
                configXML.LoadXml(config);

                GetTransactionNumberFromXml(inDocument, nodeName, ref TxElement, ref isTrxNo, configXML);

                if (string.IsNullOrEmpty(isTrxNo))
                {
                    isTrxNo = GetTransactionNumberFromCanonical(inDocument, isTrxNo);
                }
            }
            catch
            {
                isTrxNo = string.Empty;
            }

            return isTrxNo;
        }

        private static void GetTransactionNumberFromXml(XmlDocument inDocument, string nodeName, ref string TxElement,
                                                 ref string isTrxNo, XmlDocument configXML)
        {
            XmlNodeList configXmlNodeList = configXML.SelectNodes("//TransactionElements/TransactionValue");

            for (int i = 0; i < configXmlNodeList.Count; ++i)
            {
                TxElement = configXmlNodeList.Item(i).InnerText.Replace("%NODENAME%", nodeName).ToString();
                TxElement = TxElement.Remove(0, 2);
                TxElement = TxElement.Replace("/", "']/*[local-name()='");
                TxElement = "/*[local-name()='" + TxElement;
                TxElement = TxElement + "']";

                if (FindElement(inDocument, TxElement, out isTrxNo))
                {
                    break;
                }
            }
        }

        private static string GetTransactionNumberFromCanonical(XmlDocument inDocument, string isTrxNo)
        {
            MemoryStream stream = new MemoryStream();
            stream.Position = 0;

            inDocument.Save(stream);

            stream.Position = 0;

            XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings());

            while (reader.Read())
            {
                if (reader.LocalName == "MessageId")
                {
                    isTrxNo = reader.ReadString();

                    break;
                }
            }

            return isTrxNo;
        }

        private static bool FindElement(XmlDocument oXmlDocument, string NodePath, out string oxTrxNo)
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
            catch { }

            return RetValue;
        }

        private XmlDocument ValidateXml(Stream sourceStream)
        {
            XmlDocument xmlValidator = null;

            try
            {
                Encoding encoding = null;

                sourceStream.Position = 0;

                MemoryStream validationStream = new MemoryStream();

                sourceStream.CopyTo(validationStream);

                validationStream.Position = 0;

                encoding = GetEncodingFromStream(encoding, validationStream);

                try
                {
                    MemoryStream xmlStream = HandleUtf8Encoding(sourceStream, encoding);

                    xmlValidator = Utf8StreamToXml(xmlValidator, xmlStream);
                }
                catch
                {
                    try
                    {
                        string xmlString = HandleAnsiEncoding(sourceStream);

                        xmlValidator = AnsiAStreamToXml(xmlValidator, xmlString);
                    }
                    catch (Exception)
                    {
                        throw new ApplicationException("Message does not contain valid XML!");
                    }
                }
            }
            catch
            {
                xmlValidator = null;
                IsFlatFile = true;
            }

            return xmlValidator;
        }

        private static Encoding GetEncodingFromStream(Encoding encoding, Stream validationStream)
        {
            using (XmlTextReader reader = new XmlTextReader(validationStream))
            {
                reader.MoveToContent();
                encoding = reader.Encoding;
            }

            return encoding;
        }

        private static XmlDocument Utf8StreamToXml(XmlDocument xmlValidator, MemoryStream xmlStream)
        {
            xmlStream.Position = 0;

            xmlValidator = new XmlDocument();
            xmlValidator.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            xmlValidator.Load(xmlStream);

            return xmlValidator;
        }

        private static MemoryStream HandleUtf8Encoding(Stream sourceStream, Encoding encoding)
        {
            MemoryStream copyStream = new MemoryStream();

            sourceStream.Position = 0;
            sourceStream.CopyTo(copyStream);

            copyStream.Position = 0;

            byte[] streamAsByte = StreamToByte(copyStream);

            byte[] convertedByte = Encoding.Convert(encoding, Encoding.UTF8, streamAsByte);

            MemoryStream xmlStream = new MemoryStream(convertedByte);
            xmlStream.Position = 0;

            return xmlStream;
        }

        private static string HandleAnsiEncoding(Stream sourceStream)
        {
            string xmlString = string.Empty;

            MemoryStream copyStream = new MemoryStream();

            sourceStream.Position = 0;
            sourceStream.CopyTo(copyStream);

            copyStream.Position = 0;

            string lineRead = string.Empty;

            StringBuilder stringWriter = new StringBuilder();

            using (StreamReader reader = new StreamReader(copyStream, Encoding.Default))
            {
                while ((lineRead = reader.ReadLine()) != null)
                {
                   stringWriter.AppendLine(lineRead);
                }
            }

            xmlString = stringWriter.ToString();

            return xmlString;
        }

        private static XmlDocument AnsiAStreamToXml(XmlDocument xmlValidator, string xmlString)
        {
            xmlValidator = new XmlDocument();
            xmlValidator.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            xmlValidator.LoadXml(xmlString);

            return xmlValidator;
        }

        private static byte[] StreamToByte(Stream input) 
        { 
            using (MemoryStream ms = new MemoryStream()) 
            { 
                input.CopyTo(ms); 

                return ms.ToArray(); 
            } 
        } 

        private static void GetDocumentFormatAndType(IBaseMessage pInMsg, ref string documentType, ref SeekableReadOnlyStream stream, 
                                                     ref Stream sourceStream, XmlDocument inDocument, ref string sourceString, 
                                                     XmlDocument receivedDocument)
        {
            if (receivedDocument != null)
            {
                documentType = GetDocumentTypeFromXml(documentType, inDocument, receivedDocument);
            }
            else
            {
                string fileLabel = string.Empty;

                GetDocumentTypeFromBinary(pInMsg, ref documentType, ref fileLabel);
            }
        }

        private static string GetDocumentTypeFromXml(string documentType, XmlDocument inDocument, XmlDocument receivedDocument)
        {
            documentType = receivedDocument.DocumentElement.LocalName;

          if (documentType.ToLower().Equals("envelope"))
          {
              XmlNode payLoadNode = receivedDocument.SelectSingleNode("/*[local-name()='Envelope']/*[local-name()='Header']/*[local-name()='Payload']/@*[local-name()='Type']");

              if (payLoadNode != null)
              {
                  documentType = payLoadNode.Value;
              }
              else
              {
                 CouldNotFindDocumentTypeException();
              }
          }

          return documentType;
        }

        private static void GetDocumentTypeFromBinary(IBaseMessage pInMsg, ref string documentType, ref string fileLabel)
        {
            fileLabel = FilterContextPropertiesForFileLabel(pInMsg, fileLabel);

            if (!string.IsNullOrEmpty(fileLabel))
            {
                GetDocumentTypeFromFileLabel(ref documentType, ref fileLabel);
            }
            else
            {
                CouldNotFindDocumentTypeException();
            }
        }

        private static string FilterContextPropertiesForFileLabel(IBaseMessage pInMsg, string fileLabel)
        {
            if (pInMsg.Context.Read("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties") != null)
            {
                fileLabel = pInMsg.Context.Read("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties").ToString();
            }

            if (pInMsg.Context.Read("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties") != null)
            {
                fileLabel = pInMsg.Context.Read("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties").ToString();
            }

            if (pInMsg.Context.Read("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties") != null)
            {
                fileLabel = pInMsg.Context.Read("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties").ToString();
            }

            return fileLabel;
        }

        private static void GetDocumentTypeFromFileLabel(ref string documentType, ref string fileLabel)
        {
            if (fileLabel.Contains(@"\"))
            {
                string fileExtention = Path.GetExtension(fileLabel);
                string[] filePathArray = fileLabel.Split(@"\".ToCharArray());
                int filePathLen = filePathArray.Length - 1;

                string originalFileExtenstion = fileExtention;

                fileLabel = filePathArray[filePathLen].ToString().Replace(fileExtention, string.Empty);
            }

            if (fileLabel.Contains("."))
            {
                string[] fileExtentionArray = fileLabel.Split(".".ToCharArray());

                fileLabel = fileExtentionArray[0].ToString();

                string originalFileExtenstion = fileExtentionArray[1].ToString();

                if (fileLabel.Contains("_"))
                {
                    string[] fileLabelArray = fileLabel.Split("_".ToCharArray());

                    fileLabel = fileLabelArray[0].ToString();
                }
            }

            if (fileLabel.Contains("_"))
            {
                string[] fileLabelArray = fileLabel.Split("_".ToCharArray());

                fileLabel = fileLabelArray[0].ToString();
            }

            if (fileLabel.Contains("-"))
            {
                string[] fileLabelArray = fileLabel.Split("-".ToCharArray());

                fileLabel = fileLabelArray[0].ToString();
            }

            fileLabel = Cache.GetDocumentTypeFromItinerary("Itinerary", fileLabel);

            documentType = fileLabel;
        }

        private static void ApplyWindowsEncodingToXml(IBaseMessage pInMsg, ref SeekableReadOnlyStream stream, ref Stream sourceStream,
                                                      XmlDocument inDocument, ref string sourceString, Encoding oWindowsEnc)
        {
            // Get the text and byte array of the source file.
            StreamReader oStreamReader = null;

            string sSourceContent = inDocument.OuterXml;

            // Pass the source file through the XML DOM and get the parsed XML back.
            XmlDocument oParserDoc = new XmlDocument();
            oParserDoc.LoadXml(sSourceContent);
            string sParsedSourceXML = oParserDoc.OuterXml;
            byte[] bDestSourceFile = oWindowsEnc.GetBytes(sParsedSourceXML);

            // Get the XML String, write it back to a virtual stream, read it from the virtual
            // stream again.
            string sDestXML = oWindowsEnc.GetString(bDestSourceFile);
            VirtualStream oDestStream = new VirtualStream();
            StreamWriter oStreamWriter = new StreamWriter(oDestStream);
            oStreamWriter.BaseStream.Position = 0;
            oStreamWriter.BaseStream.Write(bDestSourceFile, 0, bDestSourceFile.Length);
            oStreamReader = new StreamReader(oDestStream);
            oDestStream.Position = 0;
            sourceString = oStreamReader.ReadToEnd();

            sourceStream.Position = 0;
            VirtualStream oStream = new VirtualStream();

            XmlDocument d = new XmlDocument();
            d.LoadXml(sourceString);
            d.Save(oStream);

            oStream.Position = 0;
            pInMsg.BodyPart.Data = oStream;
            stream = new SeekableReadOnlyStream(oStream);
            sourceStream = oStream;
            // END_OBERHOS_C1: Encoding change

            //remove xml declaration
            sourceString = sourceString.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
            sourceString = sourceString.Replace("<?xml version=\"1.0\" encoding=\"utf-16\" ?>", "");
            sourceString = sourceString.Replace("<?xml version=\"1.0\"?>", "");

            inDocument.LoadXml(sourceString);
        }

        private string GetDocumentTransactionNumber(string documentType, string nodeName, XmlDocument inDocument)
        {
            string txNum = ReturnTransaction(inDocument, nodeName);

            if (string.IsNullOrEmpty(txNum))
                txNum = documentType;

            return txNum;
        }

        private static void PromoteContextProperties(IBaseMessage pInMsg, string documentType, string txNum, bool isFlatFile, string originalFileName)
        {
            if (isFlatFile)
            {
                txNum = GetTransactionNumberForFlatFile(originalFileName);
            }

            pInMsg.Context.Write("TxNum", defaultContextNamespace, txNum);
            pInMsg.Context.Promote("TxNum", defaultContextNamespace, txNum);

            pInMsg.Context.Write("DocumentType", defaultContextNamespace, documentType);
            pInMsg.Context.Promote("DocumentType", defaultContextNamespace, documentType);

            pInMsg.Context.Write("NodeName", defaultContextNamespace, documentType);
            pInMsg.Context.Promote("NodeName", defaultContextNamespace, documentType);

            pInMsg.Context.Write("MaxMajorRetry", defaultContextNamespace, 0);
            pInMsg.Context.Promote("MaxMajorRetry", defaultContextNamespace, 0);

            pInMsg.Context.Write("MaxMinorRetry", defaultContextNamespace, 0);
            pInMsg.Context.Promote("MaxMinorRetry", defaultContextNamespace, 0);

            if (!string.IsNullOrEmpty(originalFileName))
            {
                pInMsg.Context.Write("ReceivedFileName","http://schemas.microsoft.com/BizTalk/2003/file-properties",originalFileName);
                pInMsg.Context.Write("ReceivedFileName","http://schemas.microsoft.com/BizTalk/2003/ftp-properties",originalFileName);
                pInMsg.Context.Write("Label","http://schemas.microsoft.com/BizTalk/2003/msmq-properties",originalFileName);

                pInMsg.Context.Promote("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties", originalFileName);
                pInMsg.Context.Promote("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties", originalFileName);
                pInMsg.Context.Promote("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", originalFileName);
            }
        }

        private static void PromoteContextPropertiesForErrorMessage(IBaseMessage pInMsg, string newTxNum)
        {
            pInMsg.Context.Write("DocumentType", defaultContextNamespace, "ERROR");
            pInMsg.Context.Promote("DocumentType", defaultContextNamespace, "ERROR");

            pInMsg.Context.Write("NodeName", defaultContextNamespace, "ERROR");
            pInMsg.Context.Promote("NodeName", defaultContextNamespace, "ERROR");

            pInMsg.Context.Write("MaxMajorRetry", defaultContextNamespace, 0);
            pInMsg.Context.Promote("MaxMajorRetry", defaultContextNamespace, 0);

            pInMsg.Context.Write("MaxMinorRetry", defaultContextNamespace, 0);
            pInMsg.Context.Promote("MaxMinorRetry", defaultContextNamespace, 0);

            pInMsg.Context.Write("TxNum", defaultContextNamespace, newTxNum);
            pInMsg.Context.Promote("TxNum", defaultContextNamespace, newTxNum);

            pInMsg.Context.Write("TrackingId", defaultContextNamespace, "ERROR");
            pInMsg.Context.Promote("TrackingId", defaultContextNamespace, "ERROR");
        }

        private static string GetTransactionNumberForFlatFile(string originalFileName)
        {
            if (originalFileName.Contains(@"\"))
            {
                string extension = Path.GetExtension(originalFileName);

                originalFileName = originalFileName.Replace(extension, "");

                string[] fileArray = originalFileName.Split(@"\".ToCharArray());
                int arrayLen = fileArray.Length - 1;

                originalFileName = fileArray[arrayLen].ToString();
            }

            return originalFileName;
        }

        private static string GetConfigurationCultureForFlatFile(string originalFileName)
        {
            if (originalFileName.Contains(@"\"))
            {
                string fileName = Path.GetFileName(originalFileName);

                originalFileName = originalFileName.Replace(fileName, "");

                string[] fileArray = originalFileName.Split(@"\".ToCharArray());
                int arrayLen = fileArray.Length - 1;

                originalFileName = fileArray[arrayLen].ToString();
            }

            return originalFileName;
        }
        #endregion

        #region LogDocs Methods
        public void WriteDocuments(Stream strm, IPipelineContext pContext, string docType, string txNo, string BTSID, IBaseMessage pInMsg,
                                   string fileExtension)
        {
            Common.WriteDocuments(strm, pContext, docType, txNo, BTSID, pInMsg, fileExtension, IsFlatFile);
        }

        public void WriteEventTracking(string trackValue)
        {
            Common.WriteEventTracking(trackValue);
        }

        private void ReadContextProperties(IPipelineContext pContext, IBaseMessage pInMsg, ref string documentType, ref string txNo,
                                           ref string TrackingId)
        {
            documentType = ContextProperties.Get(pInMsg, "DocumentType", "ERROR");
            txNo = ContextProperties.Get(pInMsg, "TxNum");

            TrackingId = System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Math.Abs(Guid.NewGuid().GetHashCode()).ToString();
        }
        #endregion

        #region Itinerary Methods
        private static Stream ExtractSourceStreamFromReceivedMessage(IBaseMessage pInMsg)
        {
            SeekableReadOnlyStream stream = new SeekableReadOnlyStream(pInMsg.BodyPart.GetOriginalDataStream());
            Stream sourceStream = pInMsg.BodyPart.GetOriginalDataStream();
            sourceStream.Position = 0;

            return sourceStream;
        }

        private string GetReceivedDocumentTypeName(IBaseMessage pInMsg)
        {
            string documentType = string.Empty;

            if (pInMsg.Context.Read("DocumentType", defaultContextNamespace) != null)
                documentType = pInMsg.Context.Read("DocumentType", defaultContextNamespace).ToString();

            return documentType;
        }

        private List<ItineraryEndpoint> GetItineraryEndpoints(XmlDocument itineraryDocument, string itineraryName, string itineraryType, 
                                                              string configurationState)
        {
            XmlNodeList resolverList = itineraryDocument.DocumentElement.GetElementsByTagName("Resolver");

            List<ItineraryEndpoint> endPoints = new List<ItineraryEndpoint>();

            foreach (XmlNode resolver in resolverList)
            {
                string route = string.Empty;
                string resolverName = resolver.Attributes["identifier"].Value;

                if (string.IsNullOrEmpty(itineraryType))
                {
                    if (itineraryName.ToLower().Contains(resolverName.ToLower()))
                    {
                        SetItineraryEndPoints(itineraryName, endPoints, resolver, resolverName, configurationState);
                    }
                }
                else
                {
                    try
                    {
                        string resoverType = resolver.Attributes["identifierType"].Value;

                        if (itineraryName.ToLower().Contains(resolverName.ToLower()) &&
                            resoverType.ToLower().Equals(itineraryType.ToLower()))
                        {
                            SetItineraryEndPoints(itineraryName, endPoints, resolver, resolverName, configurationState);
                        }
                    }
                    catch
                    {
                        if (itineraryName.ToLower().Contains(resolverName.ToLower()))
                        {
                           SetItineraryEndPoints(itineraryName, endPoints, resolver, resolverName, configurationState);
                        }
                    }
                }
            }

            return endPoints;
        }

        private static void SetItineraryEndPoints(string itineraryName, List<ItineraryEndpoint> endPoints, XmlNode resolver, 
                                                  string resolverName,  string configurationState)
        {
            XmlNodeList resolverGroups = resolver.ChildNodes;

            foreach (XmlNode resolverGroup in resolverGroups)
            {
                string route = string.Empty;
                string routingType = resolverGroup.Attributes["routingType"].Value;

                if(resolverGroup.Attributes["route"] != null)
                {
                    route = resolverGroup.Attributes["route"].Value;
                }

                if (routingType.ToLower().Equals("messaging"))
                {
                    if (string.IsNullOrEmpty(route))
                    {
                        ReturnEndPoints(itineraryName, endPoints, resolverName, resolverGroup);
                    }
                    else
                    {
                        if (route.ToLower().Equals(configurationState))
                        {
                            ReturnEndPoints(itineraryName, endPoints, resolverName, resolverGroup);
                        }
                        else
                        {
                            throw new ApplicationException("No endpoints found!");
                        }
                    }
                }
            }
        }

        private static void ReturnEndPoints(string itineraryName, List<ItineraryEndpoint> endPoints, string resolverName, XmlNode resolverGroup)
        {
            XmlNodeList endPointList = resolverGroup.ChildNodes;

            foreach (XmlNode endPointItem in endPointList)
            {
                ItineraryEndpoint endPoint = new ItineraryEndpoint();
                endPoint.Itinerary = itineraryName;
                endPoint.Resolver = resolverName;
                endPoint.Location = endPointItem.Attributes["location"].Value;
                endPoint.Protocol = endPointItem.Attributes["protocol"].Value;

                if (endPointItem.Attributes["requireTransformation"] != null)
                {
                    if (!string.IsNullOrEmpty(endPointItem.Attributes["requireTransformation"].Value))
                    {
                        endPoint.RequireTransformation = Convert.ToBoolean(endPointItem.Attributes["requireTransformation"].Value);
                    }
                }

                if (endPointItem.Attributes["transformationXSL"] != null)
                {
                    if (!string.IsNullOrEmpty(endPointItem.Attributes["transformationXSL"].Value))
                    {
                        endPoint.Transformation = endPointItem.Attributes["transformationXSL"].Value;
                    }
                }

                if (endPointItem.Attributes["requireFFDasm"] != null)
                {
                    if (!string.IsNullOrEmpty(endPointItem.Attributes["requireFFDasm"].Value))
                    {
                        endPoint.RequireFFDasm = Convert.ToBoolean(endPointItem.Attributes["requireFFDasm"].Value);
                    }
                }

                if (endPointItem.Attributes["ffDasmSchemaName"] != null)
                {
                    if (!string.IsNullOrEmpty(endPointItem.Attributes["ffDasmSchemaName"].Value))
                    {
                        endPoint.ffDasmSchemaName = endPointItem.Attributes["ffDasmSchemaName"].Value;
                    }
                }

                endPoints.Add(endPoint);
            }
        }

        private void AssignEndPointAndReturnToMessagebox(ItineraryEndpoint endPoint, IBaseMessage pInMsg, 
                                                         IPipelineContext pContext, string documentType,
                                                         Stream msgData, string itineraryType, 
                                                         string originalFileName, string interChangeId, 
                                                         string configurationState)
        {
            string endPointPath = endPoint.Location;
            string adapterType = endPoint.Protocol;

            string actionType = GetReceivedDocumentTypeName(pInMsg);

            IBaseMessage returnMsg = CreateNewReturnMessage(pInMsg, pContext, msgData);

            string txNumber = pInMsg.Context.Read("TxNum", "http://CoJ.FX/ContextProperties").ToString();

            string btsId = System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Math.Abs(Guid.NewGuid().GetHashCode()).ToString();
            string activityId = System.Guid.NewGuid().ToString();

            returnMsg.Context.Write("TrackingId", "http://CoJ.FX/ContextProperties", btsId);
            returnMsg.Context.Promote("TrackingId", "http://CoJ.FX/ContextProperties", btsId);

            returnMsg.Context.Write("InterchangeId", "http://CoJ.FX/ContextProperties", interChangeId);
            returnMsg.Context.Promote("InterchangeId", "http://CoJ.FX/ContextProperties", interChangeId);

            returnMsg.Context.Write("AdapterType", "http://CoJ.FX/ContextProperties", adapterType);
            returnMsg.Context.Promote("AdapterType", "http://CoJ.FX/ContextProperties", adapterType);


            if (!string.IsNullOrEmpty(itineraryType))
            {
                returnMsg.Context.Write("Resolver", "http://coj.esb/schemas/itinerary/properties", itineraryType);
                returnMsg.Context.Promote("Resolver", "http://coj.esb/schemas/itinerary/properties", itineraryType);
            }
            else
            {

                returnMsg.Context.Write("Resolver", "http://coj.esb/schemas/itinerary/properties", endPoint.Resolver);
                returnMsg.Context.Promote("Resolver", "http://coj.esb/schemas/itinerary/properties", endPoint.Resolver);
            }

            returnMsg.Context.Write("EndpointProtocol", "http://coj.esb/schemas/itinerary/properties", endPoint.Protocol);
            returnMsg.Context.Promote("EndpointProtocol", "http://coj.esb/schemas/itinerary/properties", endPoint.Protocol);

            returnMsg.Context.Write("DocumentType", "http://CoJ.FX/ContextProperties", documentType);
            returnMsg.Context.Promote("DocumentType", "http://CoJ.FX/ContextProperties", documentType);

            returnMsg.Context.Write("NodeName", "http://CoJ.FX/ContextProperties", documentType);
            returnMsg.Context.Promote("NodeName", "http://CoJ.FX/ContextProperties", documentType);
                
            returnMsg.Context.Write("TxNum", "http://CoJ.FX/ContextProperties", txNumber);
            returnMsg.Context.Promote("TxNum", "http://CoJ.FX/ContextProperties", txNumber);

            returnMsg.Context.Write("Miscellaneous", "http://CoJ.FX/ContextProperties", "1");
            returnMsg.Context.Promote("Miscellaneous", "http://CoJ.FX/ContextProperties", "1");

            if (!string.IsNullOrEmpty(configurationState))
            {
                returnMsg.Context.Write("Route", "http://CoJ.FX/ContextProperties", configurationState);
                returnMsg.Context.Promote("Route", "http://CoJ.FX/ContextProperties", configurationState);
            }

            if (!string.IsNullOrEmpty(originalFileName))
            {
                returnMsg.Context.Write("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties", originalFileName);
                returnMsg.Context.Write("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties", originalFileName);
                returnMsg.Context.Write("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", originalFileName);

                returnMsg.Context.Promote("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties", originalFileName);
                returnMsg.Context.Promote("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties", originalFileName);
                returnMsg.Context.Promote("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", originalFileName);
            }

            returnMsg.BodyPart.Data.Position = 0;

            IBaseMessage outputMsg = returnMsg;

            if (outputMessages == null)
            {
                outputMessages = new Queue();
            }

            outputMessages.Enqueue(outputMsg);
        }

        private IBaseMessage CreateNewReturnMessage(IBaseMessage pInMsg, IPipelineContext pContext, Stream msgData)
        {
            IBaseMessageFactory messageFactory = pContext.GetMessageFactory();
            IBaseMessage returnMsg = messageFactory.CreateMessage();
            IBaseMessagePart part = messageFactory.CreateMessagePart();

            msgData.Position = 0;
            part.Data = msgData;
            returnMsg.AddPart("Body", part, true);

            return returnMsg;
        }

        private void MessagingBasedRouting(IPipelineContext pContext, IBaseMessage pInMsg, string documentRootNode, XmlDocument itineraryDocument, 
                                           string documentType, string itineraryType, string originalFileName, string interChangeId, 
                                           string configurationState)
        {
            List<ItineraryEndpoint> endPoints = GetItineraryEndpoints(itineraryDocument, documentRootNode,itineraryType,configurationState);

            List<IBaseMessage> result = new List<IBaseMessage>();

            if (endPoints.Count > 0)
            {
                foreach (ItineraryEndpoint endPoint in endPoints)
                {
                    MemoryStream msgData = new MemoryStream();
                    msgData.Position = 0;

                    pInMsg.BodyPart.Data.Position = 0;
                    pInMsg.BodyPart.Data.CopyTo(msgData);

                    msgData.Position = 0;

                    IBaseMessage workerMessage = CreateNewReturnMessage(pInMsg, pContext, msgData);

                    if (endPoint.RequireFFDasm)
                    {
                        Microsoft.BizTalk.Component.Utilities.SchemaWithNone ffDasmSchema = new Microsoft.BizTalk.Component.Utilities.
                                                                                                SchemaWithNone(endPoint.ffDasmSchemaName);
                        FFDasmComp ffDasm = new FFDasmComp();
                        ffDasm.DocumentSpecName = ffDasmSchema;
                        ffDasm.Disassemble(pContext, workerMessage);
                        workerMessage = ffDasm.GetNext(pContext);
                    }

                    if (endPoint.RequireTransformation)
                    {
                        workerMessage = TransformMessage(pContext, workerMessage, endPoint);

                        workerMessage.BodyPart.Data.Position = 0;

                        XmlDocument transformedMessage = ValidateXml(workerMessage.BodyPart.Data);

                        itineraryType = GetItinaryTypeFromXmlBasedDocuments(transformedMessage);
                    }

                    if (endPoint.RequireRulesPolicy)
                    {
                        string rulesPolicy = endPoint.RulesPolicyName;

                        ExecuteRulesEngine(workerMessage, rulesPolicy);
                    }

                    workerMessage.BodyPart.Data.Position = 0;

                    MemoryStream resultDataStream = new MemoryStream();
                    workerMessage.BodyPart.Data.CopyTo(resultDataStream);

                    resultDataStream.Position = 0;

                    AssignEndPointAndReturnToMessagebox(endPoint, pInMsg, pContext, documentType, resultDataStream,
                                                        itineraryType, originalFileName, interChangeId, configurationState);
                }
            }
        }

        private static IBaseMessage TransformMessage(IPipelineContext pContext, IBaseMessage pInMsg, ItineraryEndpoint endPoint)
        {
            string mapName = endPoint.Transformation;
            string messageToTransform = string.Empty;

            string resultMessage = ApplyMapToMessage(pInMsg, mapName, ref messageToTransform);

            IBaseMessage transformedMessage = MappedMessageToIBase(pContext, pInMsg, resultMessage);

            pInMsg = transformedMessage;

            return pInMsg;
        }

        private static IBaseMessage MappedMessageToIBase(IPipelineContext pContext, IBaseMessage pInMsg, string resultMessage)
        {
            MemoryStream stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(resultMessage));
            stream.Position = 0;

            IBaseMessageFactory messageFactory = pContext.GetMessageFactory();
            IBaseMessage transformedMessage = messageFactory.CreateMessage();
            IBaseMessagePart part = messageFactory.CreateMessagePart();
            part.Data = stream;

            transformedMessage.AddPart("Body", part, true);

            transformedMessage.Context = pInMsg.Context;

            return transformedMessage;
        }

        private static string ApplyMapToMessage(IBaseMessage pInMsg, string mapName, ref string messageToTransform)
        {
            string lineRead = string.Empty;

            StringBuilder stringBuilder = new StringBuilder();

            using (StreamReader reader = new StreamReader(pInMsg.BodyPart.GetOriginalDataStream(),UTF8Encoding.UTF8))
            {
                while ((lineRead = reader.ReadLine()) != null)
                {
                    stringBuilder.AppendLine(lineRead);
                }
            }

            messageToTransform = stringBuilder.ToString();

            string resultMessage = Microsoft.Practices.ESB.Transform.MapHelper.TransformMessage(messageToTransform, mapName);

            return resultMessage;
        }

        private void ProcessBasedRouting(IPipelineContext pContext, IBaseMessage originalIBase, string documentRootNode, 
                                         XmlDocument itineraryDocument, string itineraryType, string documentType, 
                                         string originalFileName, string interChangeId, string configurationState)
        {
            XmlNodeList resolverList = itineraryDocument.DocumentElement.GetElementsByTagName("Resolver");

            GetProcessResolver(pContext, originalIBase, documentRootNode, resolverList, itineraryType, 
                               documentType, originalFileName,interChangeId, configurationState);
        }

        private void GetProcessResolver(IPipelineContext pContext, IBaseMessage pInMsg, string documentRootNode, 
                                        XmlNodeList resolverList, string itineraryType, string documentType, 
                                        string originalFileName, string interChangeId, string configurationState)
        {
            pInMsg.BodyPart.Data.Position = 0;

            List<IBaseMessage> result = new List<IBaseMessage>();

            foreach (XmlNode resolver in resolverList)
            {
                string route = string.Empty;
                string resolverName = resolver.Attributes["identifier"].Value;

                if (string.IsNullOrEmpty(itineraryType))
                {
                   if (documentRootNode.ToLower().Contains(resolverName.ToLower()))
                   {
                       XmlNodeList resolverGroups = resolver.ChildNodes;

                       foreach (XmlNode resolverGroup in resolverGroups)
                       {
                          string type = resolverGroup.Attributes["routingType"].Value;

                           if (type.ToLower().Equals("orchestration"))
                           {
                                PromoteAndRouteToProcess(pContext, pInMsg, resolverGroup,
                                                         pInMsg.BodyPart.Data, documentType,
                                                         originalFileName, interChangeId, resolver);
                           }
                       }
                   }
                }
                else
                {
                    try
                    {
                        string resolverType = resolver.Attributes["identifierType"].Value;

                        if (string.IsNullOrEmpty(route))
                        {
                            if (documentRootNode.ToLower().Contains(resolverName.ToLower()) &&
                                resolverType.ToLower().Equals(itineraryType.ToLower()))
                            {
                                XmlNodeList resolverGroups = resolver.ChildNodes;

                                foreach (XmlNode resolverGroup in resolverGroups)
                                {
                                    string type = resolverGroup.Attributes["routingType"].Value;

                                    if (type.ToLower().Equals("orchestration"))
                                    {
                                        PromoteAndRouteToProcess(pContext, pInMsg, resolverGroup,
                                                                 pInMsg.BodyPart.Data, documentType,
                                                                 originalFileName, interChangeId, resolver);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (documentRootNode.ToLower().Contains(resolverName.ToLower()))
                        {
                            XmlNodeList resolverGroups = resolver.ChildNodes;

                            foreach (XmlNode resolverGroup in resolverGroups)
                            {
                                string type = resolverGroup.Attributes["routingType"].Value;

                                if (type.ToLower().Equals("orchestration"))
                                {
                                    PromoteAndRouteToProcess(pContext, pInMsg, resolverGroup, 
                                                             pInMsg.BodyPart.Data, documentType,
                                                             originalFileName,interChangeId,resolver);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PromoteAndRouteToProcess(IPipelineContext pContext, IBaseMessage pInMsg, 
                                              XmlNode resolverGroup, Stream msgData, string documentType,
                                              string originalFileName,string interChangeId, XmlNode resolver)
        {
            string txNo = string.Empty;
            string activityId = System.Guid.NewGuid().ToString();
            string contextProperty = resolverGroup.Attributes["routingPropertyName"].Value;
            string contextPropertyValue = resolverGroup.Attributes["routingPropertyValue"].Value;
            string defaultContextNamespace = resolverGroup.Attributes["routingPropertyNamespace"].Value;

            try
            {
                pInMsg.BodyPart.Data.Position = 0;

                XmlDocument transDoc = new XmlDocument();
                transDoc.Load(pInMsg.BodyPart.Data);

                txNo = ReturnTransaction(transDoc,documentType);
            }
            catch
            {
                txNo = documentType;
            }

            IBaseMessage returnMsg = CreateNewReturnMessage(pInMsg, pContext, msgData);

            returnMsg.Context.Write(contextProperty, defaultContextNamespace, contextPropertyValue);
            returnMsg.Context.Promote(contextProperty, defaultContextNamespace, contextPropertyValue);

            string btsId = System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Math.Abs(Guid.NewGuid().GetHashCode()).ToString();

            returnMsg.Context.Write("TrackingId", "http://CoJ.FX/ContextProperties", btsId);
            returnMsg.Context.Promote("TrackingId", "http://CoJ.FX/ContextProperties", btsId);

            returnMsg.Context.Write("TxNum", "http://CoJ.FX/ContextProperties", txNo);
            returnMsg.Context.Promote("TxNum", "http://CoJ.FX/ContextProperties", txNo);

            returnMsg.Context.Write("EndpointProtocol", "http://coj.esb/schemas/itinerary/properties", "Orchestration");
            returnMsg.Context.Promote("EndpointProtocol", "http://coj.esb/schemas/itinerary/properties", "Orchestration");

            if (!string.IsNullOrEmpty(originalFileName))
            {
                returnMsg.Context.Write("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties", originalFileName);
                returnMsg.Context.Write("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties", originalFileName);
                returnMsg.Context.Write("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", originalFileName);

                returnMsg.Context.Promote("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/file-properties", originalFileName);
                returnMsg.Context.Promote("ReceivedFileName", "http://schemas.microsoft.com/BizTalk/2003/ftp-properties", originalFileName);
                returnMsg.Context.Promote("Label", "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", originalFileName);
            }

            if (outputMessages == null)
            {
                outputMessages = new Queue();
            }

            outputMessages.Enqueue(returnMsg);
        }
        #endregion

        #region Exception Handlers
        private static void CouldNotFindDocumentTypeException()
        {
            throw new ApplicationException("No value found to match to document type property. Document routing could not be determined!");
        }

        private static void HandleLogDocsException(IBaseMessage pInMsg, ref string documentType, ref string txNo, ref string TrackingId, Stream sourceStream)
        {
            bool bPathExists = false;
            string sPathAndFile = string.Empty;
            string sFilePath = string.Empty;
            string sRootPath = string.Empty;
            string sDate = string.Empty;
            string InOutBound = "\\InBound";

            Common.SetupLogDocsPath(ref bPathExists, ref sFilePath, ref sRootPath, ref sDate);

            Common.ReadExceptionContextProperties(pInMsg, ref documentType, ref txNo, ref TrackingId);

            Common.ManipulateExceptionPath(documentType, txNo, TrackingId, ref bPathExists, ref sPathAndFile, ref sFilePath, sRootPath, sDate, InOutBound);

            Common.ExceptionFileToDisk(sourceStream, sPathAndFile);
        }
        #endregion

        #region Rules Engine Methods
        private static void ExecuteRulesEngine(IBaseMessage pInMsg, string rulesPolicy)
        {
            Microsoft.BizTalk.RuleEngineExtensions.RuleSetDeploymentDriver rulesDriver = new Microsoft.BizTalk.RuleEngineExtensions.RuleSetDeploymentDriver();
            SqlRuleStore sqlRuleStore = (SqlRuleStore)rulesDriver.GetRuleStore();
            RuleSetInfoCollection rsic = sqlRuleStore.GetRuleSets(rulesPolicy, RuleStore.Filter.All);
            RuleSet rs = sqlRuleStore.GetRuleSet(rsic[0]);

            Microsoft.RuleEngine.RuleEngine engine = new Microsoft.RuleEngine.RuleEngine(rs);
            engine.Assert(pInMsg);
            engine.Execute();
        }

        #endregion

        #region IPersistPropertyBag
        /// <summary>
        /// Gets class ID of component for usage from unmanaged code.
        /// </summary>
        /// <param name="classid">Class ID of the component.</param>
        public void GetClassID(out Guid classid)
        {
            classid = new System.Guid("15D25754-01DB-4fee-A281-A70CEA2D09F2");
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void InitNew()
        {
        }

        /// <summary>
        /// Loads configuration property for component.
        /// </summary>
        /// <param name="pb">Configuration property bag.</param>
        /// <param name="errlog">Error status (not used in this code).</param>
        public void Load(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, Int32 errlog)
        {
        }

        /// <summary>
        /// Saves the current component configuration into the property bag.
        /// </summary>
        /// <param name="pb">Configuration property bag.</param>
        /// <param name="fClearDirty">Not used.</param>
        /// <param name="fSaveAllProperties">Not used.</param>
        public void Save(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, Boolean fClearDirty, Boolean fSaveAllProperties)
        {

        }

        /// <summary>
        /// Reads property value from property bag.
        /// </summary>
        /// <param name="pb">Property bag.</param>
        /// <param name="propName">Name of property.</param>
        /// <returns>Value of the property.</returns>
        private static object ReadPropertyBag(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, string propName)
        {
            object val = null;
            try
            {
                pb.Read(propName, out val, 0);
            }

            catch (ArgumentException)
            {
                return val;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
            return val;
        }

        /// <summary>
        /// Writes property values into a property bag.
        /// </summary>
        /// <param name="pb">Property bag.</param>
        /// <param name="propName">Name of property.</param>
        /// <param name="val">Value of property.</param>
        private static void WritePropertyBag(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, string propName, object val)
        {
            try
            {
                pb.Write(propName, ref val);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }
        #endregion

        #region IComponentUI
        /// <summary>
        /// Component icon to use in BizTalk Editor.
        /// </summary>
        [Browsable(false)]
        public IntPtr Icon
        {
            get
            {
                return System.IntPtr.Zero;
            }
        }

        /// <summary>
        /// The Validate method is called by the BizTalk Editor during the build 
        /// of a BizTalk project.
        /// </summary>
        /// <param name="obj">Project system.</param>
        /// <returns>
        /// A list of error and/or warning messages encounter during validation
        /// of this component.
        /// </returns>
        public IEnumerator Validate(object obj)
        {
            IEnumerator enumerator = null;
            ArrayList strList = new ArrayList();

            if (strList.Count > 0)
            {
                enumerator = strList.GetEnumerator();
            }

            return enumerator;
        }
        #endregion
    }
}

