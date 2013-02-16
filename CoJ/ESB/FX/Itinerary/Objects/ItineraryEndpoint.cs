namespace CoJ.ESB.FX.ITIN
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    #endregion

    [Serializable]
    public class ItineraryEndpoint
    {
        #region Properties
        public string Location
        {
            get;
            set;
        }

        public string Protocol
        {
            get;
            set;
        }

        public string Itinerary
        {
            get;
            set;
        }

        public string Resolver
        {
            get;
            set;
        }

        public bool RequireTransformation
        {
            get;
            set;
        }

        public string Transformation
        {
            get;
            set;
        }

        public bool RequireRulesPolicy
        {
            get;
            set;
        }

        public string RulesPolicyName
        {
            get;
            set;
        }

        public bool RequireFFDasm
        {
            get;
            set;
        }

        public string ffDasmSchemaName
        {
            get;
            set;
        }

        public string Route
        {
            get;
            set;
        }
        #endregion
    }
}
