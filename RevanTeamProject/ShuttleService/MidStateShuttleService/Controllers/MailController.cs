using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MidStateShuttleService.Models;
using MidStateShuttleService.Services;

namespace MidStateShuttleService.Controllers
{
    /// <summary>
    /// Handles mail item logging and reporting for drivers and admins.
    /// All actions require at minimum the Driver role via the controller-level [Authorize] attribute.
    /// Actions restricted to Admin only are further constrained with their own [Authorize] attribute.
    /// </summary>
    [Authorize(Roles = "Admin,Driver")]
    public class MailController : Controller
    {
        private readonly MailServices _mailServices;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MailController> _logger;

        public MailController(MailServices mailServices, ApplicationDbContext context, ILogger<MailController> logger)
        {
            _mailServices = mailServices;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Displays the mail item creation form. Accessible by Admin and Driver roles.
        /// </summary>
        /// <returns>The Create view initialized with an empty <see cref="MailItem"/>.</returns>
        [HttpGet]
        public IActionResult Create()
        {
            _logger.LogInformation("Mail Create page accessed.");

            // Pre-populate the location dropdown before rendering the form.
            LoadLocations();

            return View(new MailItem());
        }

        /// <summary>
        /// Handles submission of the mail item creation form and persists the new record.
        /// Accessible by Admin and Driver roles.
        /// </summary>
        /// <param name="mailItem">The mail item model populated from the submitted form.</param>
        /// <returns>
        /// Redirects back to the Create page on success, or returns the form with
        /// validation errors on invalid input.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MailItem mailItem)
        {
            _logger.LogInformation("Mail Create POST received.");

            // Validate the submitted form data before attempting to persist.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Mail Create POST failed validation.");

                // Re-populate the location dropdown since ViewBag data is not persisted across POST requests.
                LoadLocations();

                return View(mailItem);
            }

            // Stamp the submission with the current UTC time and the authenticated user's identity.
            // SubmittedBy defaults to "Unknown" if identity cannot be resolved.
            mailItem.SubmittedAt = DateTime.UtcNow;
            mailItem.SubmittedBy = User.Identity?.Name ?? "Unknown";

            _mailServices.AddMailItem(mailItem);

            _logger.LogInformation("Mail entry recorded successfully by {SubmittedBy}.", mailItem.SubmittedBy);

            TempData["SuccessMessage"] = "Mail entry recorded successfully.";

            // Redirect to Create to prevent duplicate submission on page refresh (Post/Redirect/Get pattern).
            return RedirectToAction(nameof(Create));
        }

        /// <summary>
        /// Displays a report of all recorded mail items. Restricted to Admin role only.
        /// </summary>
        /// <returns>The Report view populated with all mail item records.</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Report()
        {
            _logger.LogInformation("Mail Report accessed.");

            var mailItems = _mailServices.GetAllMailItems();

            _logger.LogInformation("Mail Report returned {MailItemCount} records.", mailItems.Count());

            return View(mailItems);
        }

        /// <summary>
        /// Populates <c>ViewBag.Locations</c> with active locations for use in the mail form dropdown.
        /// Locations are ordered alphabetically by name.
        /// </summary>
        /// <remarks>
        /// DEV NOTE: Unlike other controllers that use LocationId as the SelectListItem Value,
        /// this method uses the location Name as both Value and Text. Ensure the MailItem model
        /// stores location by name rather than ID, or align this with the ID-based approach used elsewhere.
        /// </remarks>
        private void LoadLocations()
        {
            _logger.LogInformation("Loading active locations for mail form.");

            // Filter to active locations only, ordered alphabetically for consistent dropdown presentation.
            // Note: Value is set to Name rather than LocationId
            ViewBag.Locations = _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem
                {
                    Value = l.Name,
                    Text = l.Name
                })
                .ToList();
        }
    }
}