namespace CoJ.ESB.FX.PIP.ASM.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.BizTalk.Message.Interop;

    /// <summary>
    /// A class that is responsible for reading, writing and promoting context properties.
    /// </summary>
    class ContextProperties
    {
        /// <summary>
        /// Namespaces that are currently supported by the LogDocs pipeline component.
        /// </summary>
        private static string[] Namespaces
        {
            get 
            {
                string[] result = { "http://CoJ.FX/ContextProperties" };
                return result;
            }
        }

        /// <summary>
        /// Read a context property value from a message's context.
        /// </summary>
        /// <param name="pInMsg">The message whose context will be read.</param>
        /// <param name="propertyName">The context property name.</param>
        /// <param name="defaultValue">A default value to return if the context property is not found.</param>
        /// <returns>The value of the context property.</returns>
        public static string Get(IBaseMessage pInMsg, string propertyName, string defaultValue = "")
        {
            string result = defaultValue;
            try
            {
                foreach (string namespaceValue in Namespaces)
                {
                    if (pInMsg.Context.Read(propertyName, namespaceValue) != null)
                    {
                        result = System.Convert.ToString(pInMsg.Context.Read(propertyName, namespaceValue));
                        break;
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Write and optionally promote a context property to one of the implemented schemas.
        /// </summary>
        /// <param name="pInMsg">The message, whose context will be written to.</param>
        /// <param name="propertyName">The context property name.</param>
        /// <param name="propertyValue">The context property value.</param>
        /// <param name="promoteContextProperty">A boolean flag that determines whether the written context property is promoted.</param>
        public static void Set(IBaseMessage pInMsg, string propertyName, string propertyValue, bool promoteContextProperty = false)
        {
            bool written = false;
            System.Exception exception = null;
            foreach (string namespaceValue in Namespaces)
            {
                try
                {
                    pInMsg.Context.Write(propertyName, namespaceValue, propertyValue);

                    if (promoteContextProperty)
                    {
                        pInMsg.Context.Promote(propertyName, namespaceValue, propertyValue);
                    }

                    written = true;

                    break;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
            if (!written)
            {
                if (exception == null)
                {
                }
                throw exception;
            }
        }
    }
}
