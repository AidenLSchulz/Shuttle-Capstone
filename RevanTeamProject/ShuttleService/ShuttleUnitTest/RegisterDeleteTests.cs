using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MidStateShuttleService.Controllers;
using MidStateShuttleService.Models;
using Moq;
using Xunit;
using System.Security.Claims;

public class RegisterDeleteTests
{
    private ApplicationDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private RegisterController CreateController(ApplicationDbContext db, bool isAdmin = true)
    {
        var mockLogger = new Mock<ILogger<RegisterController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new RegisterController(db, null, mockLogger.Object, cache);

        // Build a fake user with or without the Admin role
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser")
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
        );

        return controller;
    }

    private RegisterModel SeedRegistration(ApplicationDbContext db, bool isArchived = false)
    {
        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            Phone = "5555555555",
            Term = SchoolTerm.Spring,
            IsArchived = isArchived
        };

        db.RegisterModels.Add(model);
        db.SaveChanges();
        return model;
    }

    // =============================================
    // Unarchive — E012RG: Registration not found
    // =============================================

    [Fact]
    public void Unarchive_RegistrationNotFound_ReturnsE012RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var result = controller.Unarchive(9999);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal("E012RG", controller.TempData["Code"]);
    }

    // =============================================
    // Unarchive — S003RG: Successfully unarchived
    // =============================================

    [Fact]
    public void Unarchive_ValidId_ReturnsS003RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db, isArchived: true);
        var controller = CreateController(db);

        var result = controller.Unarchive(seeded.RegistrationId) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("ViewAll", result.ActionName);
        Assert.Equal("S003RG", controller.TempData["Code"]);
    }

    [Fact]
    public void Unarchive_ValidId_SetsIsArchivedFalse()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db, isArchived: true);
        var controller = CreateController(db);

        controller.Unarchive(seeded.RegistrationId);

        var updated = db.RegisterModels.Find(seeded.RegistrationId);
        Assert.NotNull(updated);
        Assert.False(updated.IsArchived);
    }

    // =============================================
    // ArchiveRegistration — E013RG: Registration not found
    // =============================================

    [Fact]
    public void ArchiveRegistration_RegistrationNotFound_ReturnsE013RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db, isAdmin: true);

        var result = controller.ArchiveRegistration(9999);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal("E013RG", controller.TempData["Code"]);
    }

    // =============================================
    // ArchiveRegistration — E014RG: User is not Admin
    // =============================================

    [Fact]
    public void ArchiveRegistration_NotAdmin_ReturnsE014RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db, isAdmin: false);

        var result = controller.ArchiveRegistration(seeded.RegistrationId);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("E014RG", controller.TempData["Code"]);
    }

    // =============================================
    // ArchiveRegistration — S004RG: Successfully archived
    // =============================================

    [Fact]
    public void ArchiveRegistration_ValidId_ReturnsS004RG()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db, isAdmin: true);

        var result = controller.ArchiveRegistration(seeded.RegistrationId) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("ViewAll", result.ActionName);
        Assert.Equal("S004RG", controller.TempData["Code"]);
    }

    [Fact]
    public void ArchiveRegistration_ValidId_SetsIsArchivedTrue()
    {
        var db = GetInMemoryDb();
        var seeded = SeedRegistration(db);
        var controller = CreateController(db, isAdmin: true);

        controller.ArchiveRegistration(seeded.RegistrationId);

        var updated = db.RegisterModels.Find(seeded.RegistrationId);
        Assert.NotNull(updated);
        Assert.True(updated.IsArchived);
    }
}