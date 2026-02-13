using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidStateShuttleService.Models;
using System.Linq;

namespace MidStateShuttleService.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult PostLoginCheck()
        {
            var oid = User.FindFirst("oid")?.Value;

            var existingUser = _context.Users
                .FirstOrDefault(u => u.AzureAdObjectId == oid);

            if (existingUser == null)
            {
                return RedirectToAction("AccountSetup");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Account()
        {
            var oidClaim = User.FindFirst("oid")
                           ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

            string userId = oidClaim?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("No Azure AD unique identifier found for the user.");

            var user = _context.Users
                .FirstOrDefault(u => u.AzureAdObjectId == userId);

            if (user == null)
                return RedirectToAction("AccountSetup");

            return View(user);
        }

        [HttpGet]
        [Authorize]
        public IActionResult AccountSetup()
        {
            // Grab the Azure AD unique identifier from claims
            var oidClaim = User.FindFirst("oid")
                           ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

            string userId = oidClaim?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                // If no unique identifier, return an error or redirect
                return Unauthorized("No Azure AD unique identifier found for the user.");
            }

            // Try to find existing user
            var user = _context.Users
                .FirstOrDefault(u => u.AzureAdObjectId == userId);

            if (user == null)
            {
                return View(new User());
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AccountSetup(User model)
        {
            var oidClaim = User.FindFirst("oid")
                           ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

            string userId = oidClaim?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No Azure AD unique identifier found for the user.");
            }

            var user = _context.Users
                .FirstOrDefault(u => u.AzureAdObjectId == userId);

            //remove the oid validation because it doesnt get passed through
            ModelState.Remove("AzureAdObjectId");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (user == null)
            {
                user = new User
                {
                    AzureAdObjectId = userId,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    StudentId = model.StudentId,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
            }
            else
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.StudentId = model.StudentId;
            }

            _context.SaveChanges();

            return RedirectToAction("Account");
        }
    }
}