namespace CoJ.ESB.FX
{
    #region Using Directives
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    #endregion

    public class CacheStore
    {
        private static Dictionary<string, object> cacheStore = null;

        #region Public Methods
        /// <summary>
        /// Add a SQL parameter object to the cache
        /// </summary>
        /// <param name="key">The unique key identifier of the cached object</param>
        /// <param name="value">The object to cache</param>
        public static void Add(string key, object value)
        {
            try
            {
                InitializeCache();

                AddValueToCache(key, value);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Return a cached SQL parameter object from the cache
        /// </summary>
        /// <param name="key">The unique identifier key of the cached object</param>
        /// <returns>Cached SQL parameter object</returns>
        public static object Get(string key)
        {
            try
            {
                return GetObjectFromCache(key);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Internal Methods
        internal static void Delete(string key)
        {
            cacheStore.Remove(key);
        }

        internal static void Clear()
        {
            cacheStore.Clear();
        }

        internal static void Update(string key, string value)
        {
            cacheStore.Remove(key);
            AddValueToCache(key, value);
        }
        #endregion

        #region Private Methods
        private static object GetObjectFromCache(string key)
        {
            if (cacheStore == null)
            {
                return null;
            }
            else
            {
                if (!cacheStore.ContainsKey(key))
                {
                    return null;
                }
                else
                {
                    return cacheStore[key];
                }
            }
        }

        private static void AddValueToCache(string key, object value)
        {
            cacheStore.Add(key, value);
        }

        private static void InitializeCache()
        {
            if (cacheStore == null)
            {
                cacheStore = new Dictionary<string, object>();
            }
        }

        #endregion
    }
}
