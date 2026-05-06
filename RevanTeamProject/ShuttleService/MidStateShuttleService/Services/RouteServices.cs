using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MidStateShuttleService.Models;
using System.Data;
using System.Xml;

namespace MidStateShuttleService.Service
{
    /// <summary>
    /// Provides data access and query operations for <see cref="Routes"/> entities.
    /// </summary>
    public class RouteServices : BaseDbServices<Routes>
    {
        public RouteServices(ApplicationDbContext dbContext) : base(dbContext, dbContext.Routes)
        {
        }

        /// <summary>
        /// Returns all routes matching the given pick-up and drop-off location IDs.
        /// </summary>
        /// <param name="pickUpId">The ID of the pick-up location to filter by.</param>
        /// <param name="dropOffId">The ID of the drop-off location to filter by.</param>
        /// <returns>A list of routes matching both location criteria, regardless of active state.</returns>
        public List<Routes> GetRoutesByLocations(int pickUpId, int dropOffId)
        {
            return _dbSet
                .Where(x => x.PickUpLocationID == pickUpId && x.DropOffLocationID == dropOffId)
                .ToList();
        }

        /// <summary>
        /// Returns all active routes with their pick-up and drop-off locations resolved,
        /// ordered by pick-up location name then pick-up time.
        /// </summary>
        /// <returns>
        /// A sorted list of active routes with <see cref="Routes.PickUpLocation"/> and
        /// <see cref="Routes.DropOffLocation"/> navigation properties populated.
        /// </returns>
        public List<Routes> GetScheduleRoutes()
        {
            List<Routes> routes = _dbSet.Where(r => r.IsActive == true).ToList();

            // Manually resolve navigation properties since Include() is not used here.
            // DEV NOTE: This should be replaced with .Include(r => r.PickUpLocation).Include(r => r.DropOffLocation)
            // to eliminate the N+1 query pattern caused by this loop.
            LocationServices ls = new LocationServices(_dbContext);
            foreach (var route in routes)
            {
                route.PickUpLocation = ls.GetEntityById(route.PickUpLocationID);
                route.DropOffLocation = ls.GetEntityById(route.DropOffLocationID);
            }

            return routes
                .OrderBy(r => r.PickUpLocation.Name)
                .ThenBy(r => r.PickUpTime)
                .ToList();
        }

        /// <summary>
        /// Returns all active routes that connect from the drop-off point of the given route,
        /// departing after the given route's drop-off time.
        /// </summary>
        /// <param name="route">The originating route whose drop-off location and time are used as the connection point.</param>
        /// <returns>
        /// A list of active routes that pick up from the given route's drop-off location
        /// after its drop-off time.
        /// </returns>
        public List<Routes> GetConnectingRoutes(Routes route)
        {
            List<Routes> routes = GetScheduleRoutes();

            // Filter to routes that depart from the given route's drop-off location after its arrival time.
            List<Routes> connectedRoutes = routes
                .Where(r => r.PickUpLocationID == route.DropOffLocationID
                        && r.PickUpTime > route.DropOffTime
                        && r.IsActive == true
                        && route.IsActive == true)
                .ToList();

            return connectedRoutes;
        }
    }
}