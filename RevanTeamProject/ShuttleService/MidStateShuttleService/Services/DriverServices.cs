using MidStateShuttleService.Models;

namespace MidStateShuttleService.Service
{
    public class DriverServices : BaseDbServices<Driver>
    {
        public DriverServices(ApplicationDbContext dbContext)
            : base(dbContext, dbContext.Drivers)
        {
        }

        /// <summary>
        /// Gets all the Drivers from the database
        /// </summary>
        public override IEnumerable<Driver> GetAllEntities()
        {
            return _dbSet
                .Where(driver => driver.IsActive)
                .ToList();
        }
    }
}