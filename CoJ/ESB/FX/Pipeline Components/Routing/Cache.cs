namespace CoJ.ESB.FX
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Management;
    using System.Threading;
    #endregion

    [Serializable]
    public class Cache
    {
        #region Public Methods
        /// <summary>
        /// Save or Insert data to the Cache store
        /// </summary>
        /// <param name="cacheInstanceIdentifier">The identifier (key) to use when inserting data in 
        /// the Cache store. This is used when retrieving the data on request.</param>
        /// <param name="cacheInstanceValue">The data to insert into the Cache store.</param>
        public static void Add(string cacheInstanceIdentifier, object cacheInstanceValue)
        {
            AddToCache(cacheInstanceIdentifier, cacheInstanceValue);
        }

        /// <summary>
        /// Get a stored value from the Cache store.
        /// </summary>
        /// <param name="cacheInstanceIdentifier">The identifier (key) to use when searching for 
        /// the requested data in the Cache store.</param>
        /// <returns>The cached value as String.</returns>
        public static object Get(string cacheInstanceIdentifier)
        {
            return GetFromCache(cacheInstanceIdentifier);
        }

        /// <summary>
        /// Validate if the cache contains a certain collection.
        /// </summary>
        /// <param name="cacheInstanceIdentifier">The collection key identifier.</param>
        /// <returns>Boolean (True / false)</returns>
        public static bool ContainsKey(string cacheInstanceIdentifier)
        {
            return KeyExists(cacheInstanceIdentifier);
        }

        /// <summary>
        /// Removes an object from the cache store.
        /// </summary>
        /// <param name="cacheInstanceIdentifier">The identifier of the cache collection to delete</param>
        public static void Delete(string cacheInstanceIdentifier)
        {
            CacheStore.Delete(cacheInstanceIdentifier);
        }

        /// <summary>
        /// Removes all items from the active cache store.
        /// </summary>
        public static void Clear()
        {
            CacheStore.Clear();
        }

        /// <summary>
        /// Update an object in the cache store.
        /// </summary>
        /// <param name="cacheInstanceIdentifier">The identifier of the cache collection to update.</param>
        /// <param name="cacheInstanceValue">The value to update in the cache store.</param>
        public static void Update(string cacheInstanceIdentifier, string cacheInstanceValue)
        {
            CacheStore.Update(cacheInstanceIdentifier, cacheInstanceValue);
        }

        public static string GetConfigSettingFromWMI(string application, string setting)
        {
            string returnValue = GetConfigurationSettingFromWMI(application, setting);

            return returnValue;
        }

        public static string GetItinerarySettingFromWMI(string application, string setting)
        {
            string configurationStorePath = @"\\localhost\root\CoJ\Configuration";
            string configurationStoreName = @"Application";
            string processName = application;
            string returnValue = string.Empty;

            return GetItineraryFromWMI(application, setting, configurationStorePath, configurationStoreName, processName, returnValue);
        }
        #endregion

        #region Internal Methods
        internal static bool KeyExists(string cacheInstanceIdentifier)
        {
            if (GetFromCache(cacheInstanceIdentifier) == null)
            {
                return false;
            }

            return true;
        }
        internal static void AddToCache(string cacheInstanceIdentifier, object cacheInstanceValue)
        {
            try
            {
                CacheStore.Add(cacheInstanceIdentifier, cacheInstanceValue);
            }
            catch {}
        }

        internal static object GetFromCache(string cacheInstanceIdentifier)
        {
            try
            {
                return CacheStore.Get(cacheInstanceIdentifier);
            }
            catch (Exception ex)
            {
               throw ex;
            }
        }
        #endregion

        #region Private Methods
        private static string GetConfigurationSettingFromWMI(string application, string setting)
        {
            string configurationStorePath = @"\\localhost\root\CoJ\Configuration";
            string configurationStoreName = @"Application";
            string processName = application;
            string returnValue = string.Empty;

            try
            {
                returnValue = (string)GetFromCache(application + "_" + setting);

                if((returnValue == null) || (String.IsNullOrEmpty(returnValue)))
                {
                    returnValue = GetSettingFromWMI(application,setting,configurationStorePath,configurationStoreName,processName,
                                                    returnValue);
                }
            }
            catch
            {
                returnValue = GetSettingFromWMI(application, setting, configurationStorePath, configurationStoreName, processName, 
                                                returnValue);
            }
            
            return returnValue;
        }

        private static string GetSettingFromWMI(string application, string setting, string configurationStorePath, 
                                                string configurationStoreName, string processName, string returnValue)
        {
            using (ManagementClass managementClass = new ManagementClass(configurationStorePath,
                                                                         configurationStoreName,
                                                                         new ObjectGetOptions()))
            {
                ManagementObjectCollection managementObjectCollection = managementClass.GetInstances();

                foreach (ManagementObject managementObject in managementObjectCollection)
                {
                    if (managementObject["Name"].ToString().ToLower() == processName.ToLower())
                    {
                        ManagementBaseObject[] baseObjects = (ManagementBaseObject[])(managementObject["Settings"]);
                        foreach (ManagementBaseObject baseObject in baseObjects)
                        {
                            string propertyName = baseObject.Properties["Name"].Value.ToString();
                            string propertyValue = baseObject.Properties["Value"].Value.ToString();
                            if (propertyName.ToLower().Contains(setting.ToLower()))
                            {
                               returnValue = propertyValue;

                               AddToCache(application + "_" + setting, returnValue);

                               break;
                            }
                        }
                    }
                }
            }

            return returnValue;
        }

        private static string GetItineraryFromWMI(string application, string setting, string configurationStorePath,
                                                string configurationStoreName, string processName, string returnValue)
        {
            using (ManagementClass managementClass = new ManagementClass(configurationStorePath,
                                                                         configurationStoreName,
                                                                         new ObjectGetOptions()))
            {
                ManagementObjectCollection managementObjectCollection = managementClass.GetInstances();

                foreach (ManagementObject managementObject in managementObjectCollection)
                {
                    if (managementObject["Name"].ToString().ToLower() == processName.ToLower())
                    {
                        ManagementBaseObject[] baseObjects = (ManagementBaseObject[])(managementObject["Settings"]);
                        foreach (ManagementBaseObject baseObject in baseObjects)
                        {
                            string propertyName = baseObject.Properties["Name"].Value.ToString();
                            string propertyValue = baseObject.Properties["Value"].Value.ToString();

                            if (setting.ToLower().Contains(propertyName.ToLower()))
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
        #endregion

        #region FlatFile Document Type From Itinerary Methods
        public static string GetDocumentTypeFromItinerary(string application, string setting)
        {
            string configurationStorePath = @"\\localhost\root\CoJ\Configuration";
            string configurationStoreName = @"Application";
            string processName = application;
            string returnValue = string.Empty;

            try
            {
                returnValue = (string)GetFromCache("DocType_" + application + "_" + setting);

                if ((returnValue == null) || (String.IsNullOrEmpty(returnValue)))
                {
                    returnValue = GetDocumentTypeFromWMI(application, setting, configurationStorePath, configurationStoreName, processName,
                                                    returnValue);
                }
            }
            catch
            {
                returnValue = GetDocumentTypeFromWMI(application, setting, configurationStorePath, configurationStoreName, processName,
                                                returnValue);
            }

            return returnValue;
        }

        private static string GetDocumentTypeFromWMI(string application, string setting, string configurationStorePath,
                                               string configurationStoreName, string processName, string returnValue)
        {
            using (ManagementClass managementClass = new ManagementClass(configurationStorePath,
                                                                         configurationStoreName,
                                                                         new ObjectGetOptions()))
            {
                ManagementObjectCollection managementObjectCollection = managementClass.GetInstances();

                foreach (ManagementObject managementObject in managementObjectCollection)
                {
                    if (managementObject["Name"].ToString().ToLower() == processName.ToLower())
                    {
                        ManagementBaseObject[] baseObjects = (ManagementBaseObject[])(managementObject["Settings"]);
                        foreach (ManagementBaseObject baseObject in baseObjects)
                        {
                            string propertyName = baseObject.Properties["Name"].Value.ToString();

                            if(setting.ToLower().ToLower().Contains(propertyName.ToLower()))
                            {
                                returnValue = propertyName;

                                AddToCache("DocType_" + application + "_" + setting, returnValue);
                                break;
                            }
                        }
                    }
                }
            }

            return returnValue;
        }
        #endregion
    }
}
