namespace CoJ.ESB.FX.ITIN
{
    #region Using Directives
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    #endregion

    [Serializable]
    public class ItineraryHelper
    {
        #region Public Methods
        /// <summary>
        /// Get the end points for the provided itinerary and resolver
        /// </summary>
        /// <param name="itineraryName">The name of the itinerary</param>
        /// <param name="resolver">The name of the linked resolver</param>
        /// <returns>Number of end points</returns>
        public static int GetEndPoints(string itineraryName, string resolver)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.Get(itineraryName, resolver);

            return endPoints.Count;
        }

        public static int GetEndPoints(string itineraryName, string resolver, string route)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.Get(itineraryName, resolver, route);

            return endPoints.Count;
        }

        /// <summary>
        /// Get the end points for the provided itinerary and resolver for orchestration only routing
        /// </summary>
        /// <param name="itineraryName">The name of the itinerary</param>
        /// <param name="resolver">The name of the linked resolver</param>
        /// <returns>Number of end points</returns>
        public static int GetEndPointsForOrchestration(string itineraryName, string resolver)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.GetEndpointsForOrchestrationRouting(itineraryName, resolver);

            return endPoints.Count;
        }

        public static int GetEndPointsForOrchestration(string itineraryName, string resolver, string route)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.GetEndpointsForOrchestrationRouting(itineraryName, resolver, route);

            return endPoints.Count;
        }

        /// <summary>
        /// Returns the end point  at the provided array index
        /// </summary>
        /// <param name="index">The array position of the end point</param>
        /// <returns>Itinerary end point</returns>
        public static ItineraryEndpoint GetEndPoint(string itineraryName, string resolver, int index)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.Get(itineraryName, resolver);

            return endPoints[index];
        }

        public static ItineraryEndpoint GetEndPoint(string itineraryName, string resolver, string route, int index)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.Get(itineraryName, resolver, route);

            return endPoints[index];
        }

        /// <summary>
        /// Returns the end point  at the provided array index for orchestration only routing
        /// </summary>
        /// <param name="index">The array position of the end point</param>
        /// <returns>Itinerary end point</returns>
        public static ItineraryEndpoint GetEndPointForOrchestration(string itineraryName, string resolver, int index)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.GetEndpointsForOrchestrationRouting(itineraryName, resolver);

            return endPoints[index];
        }

        public static ItineraryEndpoint GetEndPointForOrchestration(string itineraryName, string resolver, string route, int index)
        {
            List<ItineraryEndpoint> endPoints = Itinerary.GetEndpointsForOrchestrationRouting(itineraryName, resolver, route);

            return endPoints[index];
        }
        #endregion
    }
}
