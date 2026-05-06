using MidStateShuttleService.Models;
using System.Collections.Generic;
using System.Linq;

namespace MidStateShuttleService.Services
{
    /// <summary>
    /// Provides data access operations for <see cref="MailItem"/> entities.
    /// </summary>
    /// <remarks>
    /// DEV NOTE: Unlike most other services in this codebase, MailServices does not inherit
    /// from BaseDbServices. If generic entity operations (GetEntityById, UpdateEntity, etc.)
    /// are needed in future, consider aligning with the base class pattern used elsewhere.
    /// </remarks>
    public class MailServices
    {
        private readonly ApplicationDbContext _context;

        public MailServices(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all active mail items ordered by most recently submitted.
        /// </summary>
        /// <returns>A list of active <see cref="MailItem"/> records in descending submission order.</returns>
        public List<MailItem> GetAllMailItems()
        {
            return _context.MailItems
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.SubmittedAt)
                .ToList();
        }

        /// <summary>
        /// Returns a single active mail item by its ID, or <c>null</c> if not found or inactive.
        /// </summary>
        /// <param name="id">The ID of the mail item to retrieve.</param>
        /// <returns>The matching <see cref="MailItem"/>, or <c>null</c> if no active record exists for the given ID.</returns>
        /// <remarks>
        /// DEV NOTE: Inactive mail items are not retrievable via this method — callers will receive
        /// null for archived items even if the ID is valid. Ensure callers handle this case explicitly
        /// rather than assuming null always means the record does not exist.
        /// </remarks>
        public MailItem? GetMailItemById(int id)
        {
            return _context.MailItems
                .FirstOrDefault(m => m.MailItemId == id && m.IsActive);
        }

        /// <summary>
        /// Persists a new mail item record to the database.
        /// </summary>
        /// <param name="mailItem">The mail item entity to save.</param>
        /// <remarks>
        /// DEV NOTE: This method uses synchronous SaveChanges() while the MailController.Create
        /// action is marked async. Consider making this method async for consistency and to avoid
        /// blocking the thread on I/O.
        /// </remarks>
        public void AddMailItem(MailItem mailItem)
        {
            _context.MailItems.Add(mailItem);
            _context.SaveChanges();
        }
    }
}