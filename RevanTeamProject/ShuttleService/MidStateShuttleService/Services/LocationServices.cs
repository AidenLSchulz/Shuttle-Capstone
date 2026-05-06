using Microsoft.AspNetCore.Mvc.Rendering;
using MidStateShuttleService.Models;
using MidStateShuttleService.Service;

namespace MidStateShuttleService.Service
{
    /// <summary>
    /// Provides data access and query operations for <see cref="Location"/> entities.
    /// </summary>
    public class LocationServices : BaseDbServices<Location>
    {
        public LocationServices(ApplicationDbContext dbContext) : base(dbContext, dbContext.Locations)
        {
        }

        /// <summary>
        /// Returns all active locations as a list of <see cref="SelectListItem"/> entries
        /// for use in form dropdowns.
        /// </summary>
        /// <returns>
        /// An enumerable of <see cref="SelectListItem"/> where <c>Value</c> is the location ID
        /// and <c>Text</c> is the location name.
        /// </returns>
        public IEnumerable<SelectListItem> GetLocationNames()
        {
            var locations = new List<SelectListItem>();

            // Filter to active locations only after retrieving all entities from the base service.
            foreach (Location l in GetAllEntities().Where(l => l.IsActive))
            {
                locations.Add(new SelectListItem
                {
                    Value = l.LocationId.ToString(),
                    Text = l.Name.ToString()
                });
            }

            return locations;
        }

        /// <summary>
        /// Returns the name of a location by its ID.
        /// </summary>
        /// <param name="id">The ID of the location to look up.</param>
        /// <returns>The name of the matching location.</returns>
        public string getLocationNameById(int id)
        {
            return GetEntityById(id).Name;
        }
    }
}