using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MidStateShuttleService.Controllers;
using MidStateShuttleService.Models;
using MidStateShuttleService.Services;
using Moq;
using Xunit;
using MidStateShuttleService.Enums;

public class RegisterCreateTests
{
    private ApplicationDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private RegisterController CreateController(ApplicationDbContext db)
    {
        var mockLogger = new Mock<ILogger<RegisterController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new RegisterController(db, null, mockLogger.Object, cache);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
        );

        return controller;
    }

    // =============================================
    // E001RG — Rate limit exceeded
    // =============================================

    [Fact]
    public void Register_RateLimitExceeded_ReturnsE001RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);
        var model = new RegisterModel { Name = "John", Email = "john@test.com" };

        // Submit one past the limit (limit = 10, so 11th call triggers block)
        ViewResult lastResult = null;
        for (int i = 0; i <= 11; i++)
        {
            lastResult = controller.Register(model) as ViewResult;
        }

        Assert.NotNull(lastResult);
        Assert.Equal("E001RG", controller.TempData["Code"]);
        Assert.Equal(
            "There have been too many submissions under your internet. Please wait before trying again.",
            controller.TempData["Error"]
        );
    }

    // =============================================
    // E002RG — No rides submitted at all
    // =============================================

    [Fact]
    public void Register_NoDaySchedules_ReturnsE002RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = new List<RequestDay>() // no days = no rides
        };

        var result = controller.Register(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E002RG", controller.TempData["Code"]);
        Assert.Equal(
            "At least one ride must be added to submit a registration.",
            controller.TempData["Error"]
        );
    }

    [Fact]
    public void Register_NullDaySchedules_ReturnsE002RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = null // null also means no rides
        };

        var result = controller.Register(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E002RG", controller.TempData["Code"]);
    }

    // =============================================
    // E003RG — A request day exists but has no rides
    // =============================================

    [Fact]
    public void Register_DayScheduleWithNoRides_ReturnsE003RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>() // day present, but zero rides
                }
            }
        };

        var result = controller.Register(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E003RG", controller.TempData["Code"]);
        Assert.Equal(
            "Every request day must contain at least one ride.",
            controller.TempData["Error"]
        );
    }

    [Fact]
    public void Register_OneDayWithRidesOneDayEmpty_ReturnsE003RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 1, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                },
                new RequestDay
                {
                    WeekDay = WeekDay.Tuesday,
                    Rides = new List<Ride>() // second day is empty — should still block
                }
            }
        };

        var result = controller.Register(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E003RG", controller.TempData["Code"]);
    }

    // =============================================
    // E004RG — A ride is missing both route and drop-off time
    // =============================================

    [Fact]
    public void Register_RideMissingRouteAndTime_ReturnsE004RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride
                        {
                            RouteId = null,       // no route
                            DropOffTime = null,   // no time
                            PickUpLocationID = 1,
                            DropOffLocationID = 2
                        }
                    }
                }
            }
        };

        var result = controller.Register(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E004RG", controller.TempData["Code"]);
        Assert.Equal(
            "Each ride must have either a route selected or a drop-off time.",
            controller.TempData["Error"]
        );
    }

    [Fact]
    public void Register_RideWithRouteButNoTime_DoesNotReturnE004RG()
    {
        // A ride with a RouteId set is valid — E004RG should NOT fire
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride
                        {
                            RouteId = 1,        // route provided — valid
                            DropOffTime = null,
                            PickUpLocationID = 1,
                            DropOffLocationID = 2
                        }
                    }
                }
            }
        };

        controller.Register(model);

        Assert.NotEqual("E004RG", controller.TempData["Code"]);
    }

    // =============================================
    // E005RG — Duplicate weekdays
    // =============================================

    [Fact]
    public void Register_DuplicateDays_ReturnsE005RG()
    {
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = false,
            DaySchedules = new List<RequestDay>
            {
                new RequestDay
                {
                    WeekDay = WeekDay.Monday,
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 1, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                },
                new RequestDay
                {
                    WeekDay = WeekDay.Monday, // duplicate
                    Rides = new List<Ride>
                    {
                        new Ride { RouteId = 2, PickUpLocationID = 1, DropOffLocationID = 2 }
                    }
                }
            }
        };

        var result = controller.Register(model) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal("E005RG", controller.TempData["Code"]);
        Assert.Equal(
            "Each request day (Monday-Thursday) can only be selected once.",
            controller.TempData["Error"]
        );
    }

    // =============================================
    // Custom registration — skips ride/day validation
    // =============================================

    [Fact]
    public void Register_IsCustomTrue_SkipsRideValidation()
    {
        // isCustom bypasses E002RG and E003RG checks entirely
        var db = GetInMemoryDb();
        var controller = CreateController(db);

        var model = new RegisterModel
        {
            Name = "John",
            Email = "john@test.com",
            isCustom = true,
            DaySchedules = new List<RequestDay>() // empty — would fail if not custom
        };

        controller.Register(model);

        // Neither ride-count error should appear
        Assert.NotEqual("E002RG", controller.TempData["Code"]);
        Assert.NotEqual("E003RG", controller.TempData["Code"]);
    }
}