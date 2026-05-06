using Microsoft.EntityFrameworkCore;
using MidStateShuttleService.Models;

namespace MidStateShuttleService.Service
{

    public class CheckInServices : BaseDbServices<CheckIn>
    {
        private readonly ApplicationDbContext _context;

        public CheckInServices(ApplicationDbContext dbContext) : base(dbContext, dbContext.CheckIns)
        {
            _context = dbContext;
        }

        /// <summary>
        /// Gets all the Entities in the Database
        /// </summary>
        public override IEnumerable<CheckIn> GetAllEntities()
        {
            return _context.CheckIns
                .Include(x => x.Location)
                .Include(x => x.DropOffLocation)
                .ToList();
        }
    }
}
