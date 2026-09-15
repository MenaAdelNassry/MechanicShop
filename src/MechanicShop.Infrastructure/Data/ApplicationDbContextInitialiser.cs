using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Identity;
using MechanicShop.Domain.Inventory;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.Data;

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    TimeProvider timeProvider)
{
    private const string DefaultSeedPassword = "P@ssw0rd!1";

    private readonly ILogger<ApplicationDbContextInitialiser> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole<Guid>> _role_manager = roleManager;
    private readonly TimeProvider _timeProvider = timeProvider;

    private readonly Guid _managerEmpId = Guid.CreateVersion7();
    private readonly Guid _labor01EmpId = Guid.CreateVersion7();
    private readonly Guid _labor02EmpId = Guid.CreateVersion7();
    private readonly Guid _labor03EmpId = Guid.CreateVersion7();
    private readonly Guid _labor04EmpId = Guid.CreateVersion7();

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // 1. Seed All Roles defined in Role Enum dynamically
        foreach (var roleName in Enum.GetNames<Role>())
        {
            if (!await _role_manager.RoleExistsAsync(roleName))
            {
                await _role_manager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // 2. Seed Users
        var managerRoleName = nameof(Role.Manager);
        var laborRoleName = nameof(Role.Labor);

        var manager = new AppUser
        {
            Id = Guid.Parse("19a59129-6c20-417a-834d-11a208d32d96"),
            Email = "pm@localhost",
            UserName = "pm@localhost",
            EmailConfirmed = true,
            SecurityStamp = Guid.CreateVersion7().ToString()
        };

        if (_userManager.Users.All(u => u.Email != manager.Email))
        {
            await _userManager.CreateAsync(manager, DefaultSeedPassword);
            await _userManager.AddToRolesAsync(manager, new[] { managerRoleName });
        }

        var defaultLabors = new[]
        {
            new AppUser { Id = Guid.Parse("b6327240-0aea-46fc-863a-777fc4e42560"), Email = "john.labor@localhost", UserName = "john.labor@localhost", EmailConfirmed = true, SecurityStamp = Guid.CreateVersion7().ToString() },
            new AppUser { Id = Guid.Parse("8104ab20-26c2-4651-b1de-c0baf04dbbd9"), Email = "peter.labor@localhost", UserName = "peter.labor@localhost", EmailConfirmed = true, SecurityStamp = Guid.CreateVersion7().ToString() },
            new AppUser { Id = Guid.Parse("e17c83de-1089-4f19-bf79-5f789133d37f"), Email = "kevin.labor@localhost", UserName = "kevin.labor@localhost", EmailConfirmed = true, SecurityStamp = Guid.CreateVersion7().ToString() },
            new AppUser { Id = Guid.Parse("54cd01ba-b9ae-4c14-bab6-f3df0219ba4c"), Email = "suzan.labor@localhost", UserName = "suzan.labor@localhost", EmailConfirmed = true, SecurityStamp = Guid.CreateVersion7().ToString() }
        };

        foreach (var labor in defaultLabors)
        {
            if (_userManager.Users.All(u => u.Email != labor.Email))
            {
                await _userManager.CreateAsync(labor, DefaultSeedPassword);
                await _userManager.AddToRolesAsync(labor, new[] { laborRoleName });
            }
        }

        // 3. Seed Employees
        if (!_context.Employees.Any())
        {
            _context.Employees.AddRange([
                Employee.Create(
                    _managerEmpId,
                    PersonName.Create("Primary", "Manager").Value,
                    PhoneNumber.Create("01000000001").Value,
                    Role.Manager,
                    manager.Id).Value,

                Employee.Create(
                    _labor01EmpId,
                    PersonName.Create("John", "S.").Value,
                    PhoneNumber.Create("01000000002").Value,
                    Role.Labor,
                    defaultLabors[0].Id).Value,

                Employee.Create(
                    _labor02EmpId,
                    PersonName.Create("Peter", "R.").Value,
                    PhoneNumber.Create("01000000003").Value,
                    Role.Labor,
                    defaultLabors[1].Id).Value,

                Employee.Create(
                    _labor03EmpId,
                    PersonName.Create("Kevin", "M.").Value,
                    PhoneNumber.Create("01000000004").Value,
                    Role.Labor,
                    defaultLabors[2].Id).Value,

                Employee.Create(
                    _labor04EmpId,
                    PersonName.Create("Suzan", "L.").Value,
                    PhoneNumber.Create("01000000005").Value,
                    Role.Labor,
                    defaultLabors[3].Id).Value
            ]);
        }

        // 4. Seed Inventory Items
        var oilItemGuid = Guid.Parse("ec65225c-9066-4a1c-974f-f183c39fdd16");
        var oilFilterGuid = Guid.Parse("62ad80e3-2cff-41af-ab40-16fab8db8b38");
        var brakePadsGuid = Guid.Parse("86375a12-715e-4aa4-aad9-c0f9ccf44a14");
        var brakeFluidGuid = Guid.Parse("526d89c3-a971-4ea7-ba15-de6b50b13c21");
        var tireValveGuid = Guid.Parse("a46f974e-a198-4098-8a1f-6be6e68ec743");
        var batteryGuid = Guid.Parse("d4fd3255-29dc-4d45-9d87-f58ab98bc28b");

        if (!_context.InventoryItems.Any())
        {
            _context.InventoryItems.AddRange([
                InventoryItem.Create(oilItemGuid, "Engine Oil", 25.00m, 100, 10).Value,
                InventoryItem.Create(oilFilterGuid, "Oil Filter", 10.00m, 50, 5).Value,
                InventoryItem.Create(brakePadsGuid, "Brake Pads", 40.00m, 40, 5).Value,
                InventoryItem.Create(brakeFluidGuid, "Brake Fluid", 15.00m, 30, 5).Value,
                InventoryItem.Create(tireValveGuid, "Tire Valve", 5.00m, 200, 20).Value,
                InventoryItem.Create(batteryGuid, "Car Battery", 120.00m, 15, 2).Value
            ]);
            await _context.SaveChangesAsync();
        }

        // 5. Seed Customers & Vehicles
        if (!_context.Customers.Any())
        {
            static T EnsureSuccess<T>(Result<T> result, ILogger logger, string contextName)
            {
                if (result.IsError)
                {
                    logger.LogError("Seeding failed for {Context}: {Code} - {Description}", contextName, result.TopError.Code, result.TopError.Description);
                    throw new InvalidOperationException($"Seeding failed for {contextName}");
                }

                return result.Value;
            }

            static string ShortLicense(string prefix) => $"{prefix}-{Guid.CreateVersion7().ToString("N")[..5]}";

            Guid newCustomerId = Guid.CreateVersion7();
            Guid newCustomerId2 = Guid.CreateVersion7();

            var vehicle1 = EnsureSuccess(Vehicle.Create(Guid.Parse("61401e63-007b-4b1c-8914-9eb6e9bd95c5"), "Toyota", "Camry", 2020, ShortLicense("ABC"), newCustomerId, _timeProvider), _logger, "Vehicle1");
            var vehicle2 = EnsureSuccess(Vehicle.Create(Guid.Parse("13c80914-41ad-4d46-b7bb-60f6c89ad01e"), "Honda", "Civic", 2018, ShortLicense("XYZ"), newCustomerId, _timeProvider), _logger, "Vehicle2");

            var phone1 = EnsureSuccess(PhoneNumber.Create("01123456789"), _logger, "Phone1");
            var name1 = EnsureSuccess(PersonName.Create("John", "Doe"), _logger, "Name1");
            var email1 = EnsureSuccess(EmailAddress.Create("john.doe@localhost"), _logger, "Email1");

            var customer1 = EnsureSuccess(Customer.Create(newCustomerId, name1, email1, phone1, new List<Vehicle> { vehicle1, vehicle2 }), _logger, "Customer1");

            var v3 = EnsureSuccess(Vehicle.Create(Guid.Parse("a04f329d-0f5a-46a0-beae-699c034ae401"), "Ford", "Focus", 2021, ShortLicense("DEF"), newCustomerId2, _timeProvider), _logger, "V3");
            var v4 = EnsureSuccess(Vehicle.Create(Guid.Parse("cf60e95b-5752-4c26-aa07-31a34164606c"), "Chevrolet", "Malibu", 2019, ShortLicense("GHI"), newCustomerId2, _timeProvider), _logger, "V4");

            var phone2 = EnsureSuccess(PhoneNumber.Create("01287654321"), _logger, "Phone2");
            var name2 = EnsureSuccess(PersonName.Create("Sarah", "Peter"), _logger, "Name2");
            var email2 = EnsureSuccess(EmailAddress.Create("sarah.peter@localhost"), _logger, "Email2");

            var customer2 = EnsureSuccess(Customer.Create(newCustomerId2, name2, email2, phone2, new List<Vehicle> { v3, v4 }), _logger, "Customer2");

            _context.Customers.AddRange(customer1, customer2);
        }

        // 6. Seed Repair Tasks
        if (!_context.RepairTasks.Any())
        {
            _context.RepairTasks.AddRange([
                RepairTask.Create(Guid.Parse("616aebb1-d515-4b40-8d47-8d5c0b67a313"), "Engine Oil Change", 50.00m, RepairDurationInMinutes.Min60, [new RepairTaskPartData(oilItemGuid, 1), new RepairTaskPartData(oilFilterGuid, 1)]).Value,
                RepairTask.Create(Guid.Parse("4fa0be55-06f6-4616-b086-e1f0c9354cd8"), "Brake Replacement", 150.00m, RepairDurationInMinutes.Min90, [new RepairTaskPartData(brakePadsGuid, 2), new RepairTaskPartData(brakeFluidGuid, 1)]).Value,
                RepairTask.Create(Guid.Parse("a376b5d1-6b2d-4dd8-883e-d3d1721c1316"), "Tire Rotation", 30.00m, RepairDurationInMinutes.Min45, [new RepairTaskPartData(tireValveGuid, 4)]).Value,
                RepairTask.Create(Guid.Parse("a770cc6e-0c8b-4ac5-9ee6-6928682bd47e"), "Battery Replacement", 70.00m, RepairDurationInMinutes.Min30, [new RepairTaskPartData(batteryGuid, 1)]).Value
            ]);
        }

        ValidateAddedCustomersHavePhone(_context, _logger);

        await _context.SaveChangesAsync();

        // 7. Seed WorkOrders
        if (!_context.WorkOrders.Any())
        {
            var laborEmployeeIds = new[]
            {
                _context.Employees.FirstOrDefault(e => e.IdentityUserId == defaultLabors[0].Id)?.Id ?? _labor01EmpId,
                _context.Employees.FirstOrDefault(e => e.IdentityUserId == defaultLabors[1].Id)?.Id ?? _labor02EmpId,
                _context.Employees.FirstOrDefault(e => e.IdentityUserId == defaultLabors[2].Id)?.Id ?? _labor03EmpId,
                _context.Employees.FirstOrDefault(e => e.IdentityUserId == defaultLabors[3].Id)?.Id ?? _labor04EmpId
            };

            var workOrders = await BuildSeedWorkOrdersAsync(laborEmployeeIds);
            _context.WorkOrders.AddRange(workOrders);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<List<WorkOrder>> BuildSeedWorkOrdersAsync(Guid[] laborEmployeeIds)
    {
        var scheduled = await BuildScheduledWorkOrdersForNextMonthAsync(laborEmployeeIds);
        scheduled.AddRange(await BuildInProgressDemoWorkOrdersAsync(laborEmployeeIds));
        return scheduled;
    }

    private async Task<List<WorkOrder>> BuildScheduledWorkOrdersForNextMonthAsync(Guid[] laborEmployeeIds)
    {
        var repairTasks = await _context.RepairTasks.Include(t => t.Parts).ToListAsync();
        var vehicles = await _context.Vehicles.ToListAsync();
        var inventoryItemsMap = await _context.InventoryItems.ToDictionaryAsync(i => i.Id);

        if (repairTasks.Count == 0 || vehicles.Count == 0) return new List<WorkOrder>();

        var spots = new[] { Spot.A, Spot.B, Spot.C, Spot.D };
        var random = Random.Shared;

        TimeSpan openTime = TimeSpan.FromHours(6);
        TimeSpan closeTime = TimeSpan.FromHours(21);
        int totalMinutes = (int)(closeTime - openTime).TotalMinutes;
        int minOccupancy = (int)(totalMinutes * 0.6);
        int maxOccupancy = (int)(totalMinutes * 0.8);

        var workOrders = new List<WorkOrder>();
        var day = DateTimeOffset.Now.Date.AddDays(1);
        var lastDay = day.AddMonths(1);

        while (day < lastDay)
        {
            foreach (var spot in spots)
            {
                workOrders.AddRange(
                    FillSpotForDay(day, spot, openTime, closeTime, minOccupancy, maxOccupancy, repairTasks, vehicles, laborEmployeeIds, workOrders, random, inventoryItemsMap));
            }

            day = day.AddDays(1);
        }

        return workOrders;
    }

    private static List<WorkOrder> FillSpotForDay(
        DateTimeOffset day,
        Spot spot,
        TimeSpan openTime,
        TimeSpan closeTime,
        int minOccupancy,
        int maxOccupancy,
        List<RepairTask> repairTasks,
        List<Vehicle> vehicles,
        Guid[] laborIds,
        List<WorkOrder> existingWorkOrders,
        Random random,
        Dictionary<Guid, InventoryItem> inventoryItemsMap)
    {
        var spotWorkOrders = new List<WorkOrder>();
        int occupiedMinutes = 0;
        var currentTime = day.Add(openTime);

        while (occupiedMinutes < minOccupancy && currentTime.TimeOfDay < closeTime)
        {
            int maxTasks = Math.Min(3, repairTasks.Count);
            var selectedTasks = repairTasks
                .DistinctBy(t => t.Id)
                .OrderBy(_ => Guid.CreateVersion7())
                .Take(random.Next(1, maxTasks + 1))
                .ToList();

            int duration = selectedTasks.Sum(t => (int)t.EstimatedDurationInMins);
            if (duration <= 0)
            {
                occupiedMinutes += 30;
                currentTime = currentTime.AddMinutes(30);
                continue;
            }

            if (occupiedMinutes + duration > maxOccupancy) break;

            var startAt = currentTime;
            var endAt = startAt.AddMinutes(duration);

            var vehicle = vehicles.FirstOrDefault(v => IsVehicleFree(v.Id, startAt, endAt, existingWorkOrders, spotWorkOrders));
            if (vehicle is null)
            {
                occupiedMinutes += 30;
                currentTime = currentTime.AddMinutes(30);
                continue;
            }

            if (endAt.TimeOfDay > closeTime) break;

            var order = WorkOrder.Create(
                Guid.CreateVersion7(),
                vehicle.Id,
                startAt,
                endAt,
                laborIds[random.Next(laborIds.Length)],
                spot,
                ToWorkOrderTasks(selectedTasks, inventoryItemsMap)).Value;

            spotWorkOrders.Add(order);
            occupiedMinutes += duration;
            currentTime = day.Add(openTime).AddMinutes(occupiedMinutes);
        }

        return occupiedMinutes >= minOccupancy ? spotWorkOrders : new List<WorkOrder>();
    }

    private async Task<List<WorkOrder>> BuildInProgressDemoWorkOrdersAsync(Guid[] laborEmployeeIds)
    {
        var repairTasks = await _context.RepairTasks.Include(t => t.Parts).ToListAsync();
        var vehicleId = await _context.Vehicles.OrderBy(_ => Guid.CreateVersion7()).Select(v => v.Id).FirstAsync();
        var inventoryItemsMap = await _context.InventoryItems.ToDictionaryAsync(i => i.Id);

        var utcNow = DateTimeOffset.UtcNow;
        var tasksStartingNow = repairTasks.OrderBy(_ => Guid.CreateVersion7()).Take(2).ToList();
        var startNow = RoundToQuarterHour(utcNow);
        var endNow = startNow.AddMinutes(tasksStartingNow.Sum(t => (int)t.EstimatedDurationInMins));

        var startingNow = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicleId,
            startNow,
            endNow,
            laborEmployeeIds[0],
            Spot.A,
            ToWorkOrderTasks(tasksStartingNow, inventoryItemsMap)).Value;

        startingNow.UpdateState(WorkOrderState.InProgress, _timeProvider);

        var hourLongTask = repairTasks.First(t => t.EstimatedDurationInMins == RepairDurationInMinutes.Min60);
        var startEarlier = RoundToQuarterHour(utcNow.AddMinutes(-45));
        var endEarlier = startEarlier.AddMinutes((int)hourLongTask.EstimatedDurationInMins);

        var endingSoon = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicleId,
            startEarlier,
            endEarlier,
            laborEmployeeIds.Length > 1 ? laborEmployeeIds[1] : laborEmployeeIds[0],
            Spot.B,
            ToWorkOrderTasks(new[] { hourLongTask }, inventoryItemsMap)).Value;

        endingSoon.UpdateState(WorkOrderState.InProgress, _timeProvider);

        return new List<WorkOrder> { startingNow, endingSoon };
    }

    private static List<WorkOrderTask> ToWorkOrderTasks(IEnumerable<RepairTask> repairTasks, Dictionary<Guid, InventoryItem> inventoryItemsMap) =>
    repairTasks.Select(rt =>
    {
        var parts = rt.Parts.Select(p =>
        {
            var invItem = inventoryItemsMap.GetValueOrDefault(p.InventoryItemId);
            return new WorkOrderTaskPart(p.InventoryItemId, invItem?.Name ?? "Part", invItem?.Cost ?? 10m, p.Quantity);
        }).ToList();

        return new WorkOrderTask(
            id: Guid.CreateVersion7(),
            originalTaskId: rt.Id,
            name: rt.Name,
            laborCost: rt.LaborCost,
            estimatedDurationInMins: rt.EstimatedDurationInMins,
            parts: parts);
    }).ToList();

    private static bool IsVehicleFree(
        Guid vehicleId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        IEnumerable<WorkOrder> committed,
        IEnumerable<WorkOrder> pendingForSpot) =>
        !committed.Concat(pendingForSpot).Any(w =>
            w.VehicleId == vehicleId &&
            w.StartAtUtc.Date == startAt.Date &&
            w.StartAtUtc < endAt &&
            w.EndAtUtc > startAt);

    private static DateTimeOffset RoundToQuarterHour(DateTimeOffset time)
    {
        int minute = time.Minute - (time.Minute % 15);
        return new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, minute, 0, time.Offset);
    }

    private static void ValidateAddedCustomersHavePhone(AppDbContext ctx, ILogger logger)
    {
        var addedCustomers = ctx.ChangeTracker.Entries<Customer>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var c in addedCustomers)
        {
            if (c == null || c.PhoneNumber == null)
            {
                logger.LogError("Seeding failed: Customer {CustomerId} has null PhoneNumber.", c?.Id);
                throw new InvalidOperationException($"Seeding failed: Customer {c?.Id} has null PhoneNumber.");
            }
        }
    }
}

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}