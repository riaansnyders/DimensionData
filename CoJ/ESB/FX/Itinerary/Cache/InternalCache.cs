namespace CoJ.ESB.FX.ITIN
{
    #region Using Directives
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    #endregion

    public class InternalCache
    {
        public static Dictionary<string, object> internalCache = null;

        #region Public Methods
        /// <summary>
        /// Add a key value pair to the internal cache
        /// </summary>
        /// <param name="key">Key lookup of the value to add to the cache</param>
        /// <param name="value">The value to add to the cache</param>
        public static void Add(string key, object value)
        {
            if (internalCache == null)
            {
                internalCache = new Dictionary<string, object>();
            }

            if (internalCache.ContainsKey(key))
            {
                internalCache.Remove(key);

                System.Threading.Thread.Sleep(500);
            }

            try
            {
                internalCache.Add(key, value);
            }
            catch
            {
                //Ignore
            }
        }

        /// <summary>
        /// Returns a value for the matching key from the cache
        /// </summary>
        /// <param name="key">The lookup key</param>
        /// <returns>The value associated with the provided key</returns>
        public static object Get(string key)
        {
            return internalCache[key].ToString();

        }

        /// <summary>
        /// Check if the cache contains the requested value
        /// </summary>
        /// <param name="key">The key to search for in the cache store</param>
        /// <returns>Boolean indicating if the key does exists</returns>
        public static bool Contains(string key)
        {
            bool isInCache = false;

            try
            {
                internalCache[key].ToString();
                isInCache = true;
            }
            catch
            {
                isInCache = false;
            }

            return isInCache;
        }
        #endregion
    }
}
