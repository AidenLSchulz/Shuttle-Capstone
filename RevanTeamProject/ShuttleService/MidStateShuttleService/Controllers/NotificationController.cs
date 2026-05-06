using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MidStateShuttleService.Models;

namespace MidStateShuttleService.Controllers
{
    /// <summary>
    /// Handles creation, archiving, and routing of admin notifications.
    /// Notifications are linked to specific entities (feedback, messages, registrations)
    /// and route the admin to the relevant controller action when viewed.
    /// </summary>
    /// <remarks>
    /// DEV NOTE: This controller has no class-level [Authorize] attribute.
    /// <see cref="ViewNotificationContents"/> is publicly accessible without authentication.
    /// Consider adding a class-level [Authorize] attribute and using [AllowAnonymous] selectively
    /// if any actions are intentionally public.
    /// </remarks>
    public class NotificationController : Controller
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public NotificationController(
            ApplicationDbContext context,
            ILogger<NotificationController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Creates a new notification record from a JSON request body and persists it to the database.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="notification">The notification model deserialized from the request body.</param>
        /// <returns>
        /// 200 OK with the created notification on success, 400 Bad Request if the body is null,
        /// or 500 Internal Server Error on an unhandled exception.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This action accepts JSON via [FromBody] unlike most other actions in this
        /// codebase that use form binding. It is likely called from JavaScript rather than a
        /// standard form submission.
        /// </remarks>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Notification notification)
        {
            _logger.LogInformation("Notification Create POST received.");

            // Guard against a null body — returned when the request body is missing or malformed.
            if (notification == null)
            {
                _logger.LogWarning("Notification Create failed because notification was null.");
                return BadRequest("Notification is null");
            }

            try
            {
                // Stamp the notification with the current time and default archive state at creation.
                notification.TimeSent = DateTime.Now;
                notification.IsArchived = false;

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification created successfully with Id: {NotificationId}", notification.Id);

                // Return the persisted notification so the caller can access the generated ID.
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Soft-archives a notification by setting its <c>IsArchived</c> flag to <c>true</c>.
        /// Restricted to Admin role only.
        /// </summary>
        /// <param name="id">The ID of the notification to archive.</param>
        /// <returns>
        /// Redirects to the Dashboard Index in all cases — on success, not found, and exception.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This action redirects to the Dashboard regardless of outcome, including
        /// when the notification is not found. Consider surfacing a TempData error message on
        /// failure so the admin has feedback that the archive operation did not succeed.
        /// </remarks>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Archive(int id)
        {
            _logger.LogInformation("Notification Archive requested for Id: {NotificationId}", id);

            var notification = await _context.Notifications.FindAsync(id);

            // Redirect to the Dashboard if the notification no longer exists.
            if (notification == null)
            {
                _logger.LogWarning("Notification Archive failed. Notification not found for Id: {NotificationId}", id);
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                // Soft-archive the notification — the record is retained but hidden from active views.
                notification.IsArchived = true;

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification archived successfully for Id: {NotificationId}", id);

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                // Any persistence failure lands here — redirect to the Dashboard without surfacing detail.
                _logger.LogError(ex, "Error archiving notification");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Routes the admin to the relevant controller action based on the entity linked to the notification.
        /// Acts as a dispatcher — inspecting the notification's linked IDs to determine the redirect target.
        /// </summary>
        /// <param name="id">The ID of the notification to view.</param>
        /// <returns>
        /// Redirects to the Feedback, Communicate, or Register controller based on which linked ID
        /// is populated. Redirects to the Dashboard Index if no linked entity is found or the
        /// notification does not exist.
        /// </returns>
        /// <remarks>
        /// DEV NOTE: This action has no [HttpGet] attribute and no [Authorize] attribute,
        /// making it unauthenticated and accessible via any HTTP method. Both should be added.
        /// </remarks>
        public IActionResult ViewNotificationContents(int id)
        {
            _logger.LogInformation("ViewNotificationContents requested for Id: {NotificationId}", id);

            var notification = _context.Notifications
                .FirstOrDefault(n => n.Id == id);

            // Redirect to the Dashboard if the notification record cannot be found.
            if (notification == null)
            {
                _logger.LogWarning("ViewNotificationContents failed. Notification not found for Id: {NotificationId}", id);
                return RedirectToAction("Index", "Dashboard");
            }

            // Route to Feedback ViewAll if this notification is linked to a feedback submission.
            if (notification.FeedbackId.HasValue && notification.FeedbackId.Value != 0)
            {
                _logger.LogInformation("Notification {NotificationId} redirected to Feedback ViewAll.", id);
                return RedirectToAction("ViewAll", "Feedback");
            }

            // Route to Communicate ViewAll if this notification is linked to a student message.
            if (notification.MessageId.HasValue && notification.MessageId.Value != 0)
            {
                _logger.LogInformation("Notification {NotificationId} redirected to Communicate ViewAll.", id);
                return RedirectToAction("ViewAll", "Communicate");
            }

            // Route to the Register Details page if this notification is linked to a registration.
            if (notification.RegistrationId.HasValue && notification.RegistrationId.Value != 0)
            {
                _logger.LogInformation(
                    "Notification {NotificationId} redirected to Register Details for RegistrationId: {RegistrationId}",
                    id, notification.RegistrationId);

                return RedirectToAction(
                    "Details",
                    "Register",
                    new { registrationId = notification.RegistrationId }
                );
            }

            // No linked entity was found on the notification — fall back to the Dashboard.
            _logger.LogInformation("Notification {NotificationId} had no linked entity. Redirecting to Dashboard Index.", id);

            return RedirectToAction("Index", "Dashboard");
        }
    }
}