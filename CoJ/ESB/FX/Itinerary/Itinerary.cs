namespace CoJ.ESB.FX.ITIN
{
    #region Using Directives
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;
    using System.Text;
    using System.Xml;
    #endregion

    [Serializable]
    public class Itinerary    
    {
        #region Public Methods
        /// <summary>
        /// Return destination endpoints for provided itinerary
        /// </summary>
        /// <param name="itineraryName">The itinerary to use when searching for end points</param>
        /// <param name="resolver">The resolver mapped to the itinerary</param>
        /// <returns>List of destination end points</returns>
        public static List<ItineraryEndpoint> Get(string itineraryName, string resolver)
        {
            List<ItineraryEndpoint> endPoints = new List<ItineraryEndpoint>();

            XmlDocument itineraryXml = GetItineraryXML(itineraryName,false);

            XmlNodeList itineraries = itineraryXml.DocumentElement.GetElementsByTagName("Resolver");

            GetEndPoints(itineraryName, resolver, string.Empty, endPoints, itineraries);

            return endPoints;
        }

        public static List<ItineraryEndpoint> Get(string itineraryName, string resolver, string route)
        {
            List<ItineraryEndpoint> endPoints = new List<ItineraryEndpoint>();

            XmlDocument itineraryXml = GetItineraryXML(itineraryName, false);

            XmlNodeList itineraries = itineraryXml.DocumentElement.GetElementsByTagName("Resolver");

            GetEndPoints(itineraryName, route, resolver, endPoints, itineraries);

            return endPoints;
        }

        public static List<ItineraryEndpoint> GetEndpointsForOrchestrationRouting(string itineraryName, string resolver)
        {
            List<ItineraryEndpoint> endPoints = new List<ItineraryEndpoint>();

            XmlDocument itineraryXml = GetItineraryXML(itineraryName, true);

            XmlNodeList itineraries = itineraryXml.DocumentElement.GetElementsByTagName("Resolver");

            GetEndPoints(itineraryName, resolver, string.Empty, endPoints, itineraries);

            return endPoints;
        }

        public static List<ItineraryEndpoint> GetEndpointsForOrchestrationRouting(string itineraryName, string resolver, string route)
        {
            List<ItineraryEndpoint> endPoints = new List<ItineraryEndpoint>();

            XmlDocument itineraryXml = GetItineraryXML(itineraryName,true);

            XmlNodeList itineraries = itineraryXml.DocumentElement.GetElementsByTagName("Resolver");

            GetEndPoints(itineraryName, route, resolver, endPoints, itineraries);

            return endPoints;
        }
        #endregion

        #region Private Methods
        private static void GetEndPoints(string itineraryName, string resolver, string route, 
                                         List<ItineraryEndpoint> endPoints, XmlNodeList itineraries)
        {
            foreach (XmlNode itinerary in itineraries)
            {
                if (itinerary.Attributes["identifier"].Value == resolver)
                {
                    XmlNodeList resolverGroups = itinerary.ChildNodes;

                    foreach (XmlNode resolverGroup in resolverGroups)
                    {
                        if (resolverGroup.Attributes["routingType"].Value == "Messaging")
                        {
                            if(!string.IsNullOrEmpty(route))
                            {
                                if (resolverGroup.Attributes["route"].Value.ToLower() == route.ToLower())
                                {
                                    XmlNodeList endPointNodes = resolverGroup.ChildNodes;

                                    foreach (XmlNode endPoint in endPointNodes)
                                    {
                                        ItineraryEndpoint itineraryEndPoint = new ItineraryEndpoint();
                                        itineraryEndPoint.Itinerary = itineraryName;
                                        itineraryEndPoint.Location = endPoint.Attributes["location"].Value;
                                        itineraryEndPoint.Protocol = endPoint.Attributes["protocol"].Value;
                                        itineraryEndPoint.Resolver = resolver;

                                        endPoints.Add(itineraryEndPoint);
                                    }
                                }
                            }
                            else
                            {
                                XmlNodeList endPointNodes = resolverGroup.ChildNodes;

                                foreach (XmlNode endPoint in endPointNodes)
                                {
                                   ItineraryEndpoint itineraryEndPoint = new ItineraryEndpoint();
                                   itineraryEndPoint.Itinerary = itineraryName;
                                   itineraryEndPoint.Location = endPoint.Attributes["location"].Value;
                                   itineraryEndPoint.Protocol = endPoint.Attributes["protocol"].Value;
                                   itineraryEndPoint.Resolver = resolver;

                                   endPoints.Add(itineraryEndPoint);
                               }
                           }
                        }
                    }
                }
            }
        }

        private static XmlDocument GetItineraryXML(string itineraryName, bool IsOrchestration)
        {
            XmlDocument itineraryXML = new XmlDocument();

            string returnValue = string.Empty;
            string configurationStorePath = Constants.ITINERARYSTOREPATH;
            string configurationStoreName = Constants.ITINERARYSTORENAME;

            if (!IsOrchestration)
            {
                returnValue = GetItiniraryFromConfiguration(itineraryName, returnValue, configurationStorePath, configurationStoreName);
            }
            else
            {
                returnValue = GetItiniraryFromConfigurationForOrchestration(itineraryName, returnValue, configurationStorePath, 
                                                                            configurationStoreName);
            }

            ItineraryStringToXml(itineraryXML, returnValue);

            return itineraryXML;
        }

        private static string GetItiniraryFromConfiguration(string itineraryName, string returnValue, string configurationStorePath, string configurationStoreName)
        {
            using (ManagementClass managementClass = new ManagementClass(configurationStorePath, configurationStoreName, new ObjectGetOptions()))
            {
                ManagementObjectCollection collection = managementClass.GetInstances();

                foreach (ManagementObject managementObject in collection)
                {
                    if (managementObject["Name"].ToString().ToLower() == "itinerary")
                    {
                        ManagementBaseObject[] baseObjects = (ManagementBaseObject[])(managementObject["Settings"]);

                        foreach (ManagementBaseObject baseObject in baseObjects)
                        {
                            string propertyName = baseObject.Properties["Name"].Value.ToString();
                            string propertyValue = baseObject.Properties["Value"].Value.ToString();

                            if (propertyName.ToUpper() == itineraryName.ToUpper())
                            {
                                returnValue = propertyValue;
                                break;
                            }
                        }
                    }
                }
            }

            return returnValue;
        }

        private static string GetItiniraryFromConfigurationForOrchestration(string itineraryName, string returnValue, string configurationStorePath, string configurationStoreName)
        {
            using (ManagementClass managementClass = new ManagementClass(configurationStorePath, configurationStoreName, new ObjectGetOptions()))
            {
                ManagementObjectCollection collection = managementClass.GetInstances();

                foreach (ManagementObject managementObject in collection)
                {
                    if (managementObject["Name"].ToString().ToLower() == "itineraryorchestration")
                    {
                        ManagementBaseObject[] baseObjects = (ManagementBaseObject[])(managementObject["Settings"]);

                        foreach (ManagementBaseObject baseObject in baseObjects)
                        {
                            string propertyName = baseObject.Properties["Name"].Value.ToString();
                            string propertyValue = baseObject.Properties["Value"].Value.ToString();

                            if (propertyName.ToUpper() == itineraryName.ToUpper())
                            {
                                returnValue = propertyValue;
                                break;
                            }
                        }
                    }
                }
            }

            return returnValue;
        }
        private static void ItineraryStringToXml(XmlDocument itineraryXML, string returnValue)
        {
            if (!String.IsNullOrEmpty(returnValue))
            {
                itineraryXML.LoadXml(returnValue);
            }
        }
        #endregion
    }
}
