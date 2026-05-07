using MidStateShuttleService.Models;
using MidStateShuttleService.Service;

namespace MidStateShuttleService.Services
{
    /// <summary>
    /// Provides data access operations for <see cref="Notification"/> entities.
    /// </summary>
    public class NotificationService : BaseDbServices<Notification>
    {
        private ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext dbContext) : base(dbContext, dbContext.Notifications)
        {
            _context = dbContext;
        }

        /// <summary>
        /// Persists a new notification record to the database.
        /// </summary>
        /// <param name="notification">The notification entity to save.</param>
        public void SendNotification(Notification notification)
        {
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }
    }
}