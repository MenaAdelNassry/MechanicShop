using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Identity;
using MechanicShop.Domain.Inventory;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.Spots;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Enums;
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

    private readonly Guid _managerEmpId = Guid.Parse("0191e4a0-0000-7000-8000-000000000001");
    private readonly Guid _labor01EmpId = Guid.Parse("0191e4a0-0000-7000-8000-000000000002");
    private readonly Guid _labor02EmpId = Guid.Parse("0191e4a0-0000-7000-8000-000000000003");
    private readonly Guid _labor03EmpId = Guid.Parse("0191e4a0-0000-7000-8000-000000000004");
    private readonly Guid _labor04EmpId = Guid.Parse("0191e4a0-0000-7000-8000-000000000005");

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
        // 1. Seed Roles
        foreach (var roleName in Enum.GetNames<Role>())
        {
            if (!await _role_manager.RoleExistsAsync(roleName))
            {
                await _role_manager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // 2. Seed Identity Users
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
                Employee.Create(_managerEmpId, PersonName.Create("Ahmed", "Hassan").Value, PhoneNumber.Create("01000000001").Value, Role.Manager, manager.Id).Value,
                Employee.Create(_labor01EmpId, PersonName.Create("John", "Smith").Value, PhoneNumber.Create("01000000002").Value, Role.Labor, defaultLabors[0].Id).Value,
                Employee.Create(_labor02EmpId, PersonName.Create("Peter", "Ramzy").Value, PhoneNumber.Create("01000000003").Value, Role.Labor, defaultLabors[1].Id).Value,
                Employee.Create(_labor03EmpId, PersonName.Create("Kevin", "Maged").Value, PhoneNumber.Create("01000000004").Value, Role.Labor, defaultLabors[2].Id).Value,
                Employee.Create(_labor04EmpId, PersonName.Create("Suzan", "Lotfy").Value, PhoneNumber.Create("01000000005").Value, Role.Labor, defaultLabors[3].Id).Value
            ]);
            await _context.SaveChangesAsync();
        }

        // 4. Seed Service Bays (Spots)
        if (!_context.ServiceBays.Any())
        {
            _context.ServiceBays.AddRange([
                ServiceBay.Create("Bay A", "General Mechanical Bay 1"),
                ServiceBay.Create("Bay B", "General Mechanical Bay 2"),
                ServiceBay.Create("Bay C", "Quick Service & Oil Bay"),
                ServiceBay.Create("Bay D", "Brakes & Suspension Bay")
            ]);
            await _context.SaveChangesAsync();
        }

        // 5. Seed Inventory Items
        var oilItemGuid = Guid.Parse("ec65225c-9066-4a1c-974f-f183c39fdd16");
        var oilFilterGuid = Guid.Parse("62ad80e3-2cff-41af-ab40-16fab8db8b38");
        var brakePadsGuid = Guid.Parse("86375a12-715e-4aa4-aad9-c0f9ccf44a14");
        var brakeFluidGuid = Guid.Parse("526d89c3-a971-4ea7-ba15-de6b50b13c21");
        var tireValveGuid = Guid.Parse("a46f974e-a198-4098-8a1f-6be6e68ec743");
        var batteryGuid = Guid.Parse("d4fd3255-29dc-4d45-9d87-f58ab98bc28b");

        if (!_context.InventoryItems.Any())
        {
            _context.InventoryItems.AddRange([
                InventoryItem.Create(oilItemGuid, "Synthetic 5W-30 Oil (4L)", 45.00m, 120, 15).Value,
                InventoryItem.Create(oilFilterGuid, "OEM Oil Filter", 15.00m, 60, 10).Value,
                InventoryItem.Create(brakePadsGuid, "Front Ceramic Brake Pads", 85.00m, 40, 8).Value,
                InventoryItem.Create(brakeFluidGuid, "DOT4 Brake Fluid 500ml", 18.00m, 35, 5).Value,
                InventoryItem.Create(tireValveGuid, "High-Pressure Tire Valve", 6.00m, 150, 20).Value,
                InventoryItem.Create(batteryGuid, "70Ah Lead-Acid Battery", 140.00m, 20, 4).Value
            ]);
            await _context.SaveChangesAsync();
        }

        // 6. Seed Repair Tasks
        var oilChangeTaskId = Guid.Parse("616aebb1-d515-4b40-8d47-8d5c0b67a313");
        var brakeChangeTaskId = Guid.Parse("4fa0be55-06f6-4616-b086-e1f0c9354cd8");
        var tireRotationTaskId = Guid.Parse("a376b5d1-6b2d-4dd8-883e-d3d1721c1316");
        var batteryChangeTaskId = Guid.Parse("a770cc6e-0c8b-4ac5-9ee6-6928682bd47e");

        if (!_context.RepairTasks.Any())
        {
            _context.RepairTasks.AddRange([
                RepairTask.Create(oilChangeTaskId, "Engine Oil & Filter Service", 40.00m, RepairDurationInMinutes.Min45, [new RepairTaskPartData(oilItemGuid, 1), new RepairTaskPartData(oilFilterGuid, 1)]).Value,
                RepairTask.Create(brakeChangeTaskId, "Brake Pads Replacement & Bleeding", 75.00m, RepairDurationInMinutes.Min60, [new RepairTaskPartData(brakePadsGuid, 1), new RepairTaskPartData(brakeFluidGuid, 1)]).Value,
                RepairTask.Create(tireRotationTaskId, "Tire Balancing & Rotation", 25.00m, RepairDurationInMinutes.Min30, [new RepairTaskPartData(tireValveGuid, 4)]).Value,
                RepairTask.Create(batteryChangeTaskId, "Battery Replacement & Terminal Cleaning", 30.00m, RepairDurationInMinutes.Min30, [new RepairTaskPartData(batteryGuid, 1)]).Value
            ]);
            await _context.SaveChangesAsync();
        }

        // 7. Seed Customers & Vehicles (Distinct Customers with Realistic Vehicles)
        if (!_context.Customers.Any())
        {
            var seedCustomers = new List<Customer>();

            var customersData = new (string First, string Last, string Phone, string Email, string Make, string Model, int Year, string Plate)[]
            {
                ("Maged", "Nabil", "01011112222", "maged.nabil@example.com", "Toyota", "Corolla", 2021, "ABC-1234"),
                ("Ramy", "Tawfik", "01133334444", "ramy.tawfik@example.com", "Hyundai", "Elantra", 2019, "XYZ-5678"),
                ("Sherif", "Adel", "01255556666", "sherif.adel@example.com", "Kia", "Sportage", 2022, "DEF-9012"),
                ("Tamer", "Hosny", "01077778888", "tamer.hosny@example.com", "Nissan", "Sunny", 2018, "GHI-3456"),
                ("Hany", "Salama", "01599990000", "hany.salama@example.com", "BMW", "320i", 2020, "JKL-7890"),
                ("Mina", "George", "01022223333", "mina.george@example.com", "Renault", "Megane", 2021, "MNO-2345"),
                ("Karim", "Fahmy", "01144445555", "karim.fahmy@example.com", "Mercedes-Benz", "C180", 2022, "PQR-6789"),
                ("Omar", "Youssef", "01266667777", "omar.youssef@example.com", "Skoda", "Octavia", 2023, "STU-0123")
            };

            foreach (var item in customersData)
            {
                var custId = Guid.CreateVersion7();
                var vehicle = Vehicle.Create(Guid.CreateVersion7(), item.Make, item.Model, item.Year, item.Plate, custId, _timeProvider).Value;
                var cust = Customer.Create(
                    custId,
                    PersonName.Create(item.First, item.Last).Value,
                    EmailAddress.Create(item.Email).Value,
                    PhoneNumber.Create(item.Phone).Value,
                    new List<Vehicle> { vehicle }).Value;

                seedCustomers.Add(cust);
            }

            _context.Customers.AddRange(seedCustomers);
            await _context.SaveChangesAsync();
        }

        // 8. Seed Realistic 2-Day Work Orders
        if (!_context.WorkOrders.Any())
        {
            await SeedTwoDayRealisticWorkOrdersAsync();
        }
    }

    private async Task SeedTwoDayRealisticWorkOrdersAsync()
    {
        var bays = await _context.ServiceBays.OrderBy(b => b.Name).ToListAsync();
        var labors = await _context.Employees.Where(e => e.Role == Role.Labor).OrderBy(e => e.Name.FirstName).ToListAsync();
        var vehicles = await _context.Vehicles.OrderBy(v => v.LicensePlate).ToListAsync();
        var tasks = await _context.RepairTasks.Include(t => t.Parts).ToDictionaryAsync(t => t.Id);
        var invItems = await _context.InventoryItems.ToDictionaryAsync(i => i.Id);

        if (bays.Count < 4 || labors.Count < 4 || vehicles.Count < 8) return;

        var nowUtc = _timeProvider.GetUtcNow();
        var today = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);
        var tomorrow = today.AddDays(1);

        var oilTask = tasks[Guid.Parse("616aebb1-d515-4b40-8d47-8d5c0b67a313")];
        var brakeTask = tasks[Guid.Parse("4fa0be55-06f6-4616-b086-e1f0c9354cd8")];
        var tireTask = tasks[Guid.Parse("a376b5d1-6b2d-4dd8-883e-d3d1721c1316")];
        var batteryTask = tasks[Guid.Parse("a770cc6e-0c8b-4ac5-9ee6-6928682bd47e")];

        var seededOrders = new List<WorkOrder>();

        // ==========================================
        // ????? ????? (Today): 4 ????? ?????? ??????
        // ==========================================

        // 1. ??? ????? ????? ????? ??????? ?? ???????
        var wo1Start = today.AddHours(8).AddMinutes(30);
        var wo1End = wo1Start.AddMinutes((int)oilTask.EstimatedDurationInMins);
        var wo1 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[0].Id,
            wo1Start,
            wo1End,
            labors[0].Id,
            bays[0].Id,
            ToWorkOrderTasks([oilTask], invItems)).Value;

        wo1.UpdateState(WorkOrderState.InProgress, _timeProvider);
        wo1.UpdateState(WorkOrderState.Completed, _timeProvider);
        seededOrders.Add(wo1);

        // ?????? ????? ????? ?????
        var invoice1 = Invoice.Create(
            Guid.CreateVersion7(),
            wo1.Id,
            [
                InvoiceLineItem.Create(Guid.Empty, 1, $"Service: {oilTask.Name}", 1, oilTask.LaborCost).Value,
                InvoiceLineItem.Create(Guid.Empty, 2, "Synthetic 5W-30 Oil", 1, 45.00m).Value,
                InvoiceLineItem.Create(Guid.Empty, 3, "OEM Oil Filter", 1, 15.00m).Value
            ],
            discountAmount: 0m,
            _timeProvider).Value;

        invoice1.RecordPayment(
            Guid.CreateVersion7(),
            invoice1.Total,
            PaymentMethod.PosCard,
            labors[0].Id,
            "POS-AUTH-98214",
            _timeProvider);

        _context.Invoices.Add(invoice1);

        // 2. ??? ??? ??????? ?????? (InProgress) ?? ?????? B
        var wo2Start = today.AddHours(10).AddMinutes(0);
        var wo2End = wo2Start.AddMinutes((int)brakeTask.EstimatedDurationInMins);
        var wo2 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[1].Id,
            wo2Start,
            wo2End,
            labors[1].Id,
            bays[1].Id,
            ToWorkOrderTasks([brakeTask], invItems)).Value;

        wo2.UpdateState(WorkOrderState.InProgress, _timeProvider);
        seededOrders.Add(wo2);

        // 3. ??? ????? ????? ??????? (Scheduled)
        var wo3Start = today.AddHours(13).AddMinutes(0);
        var wo3End = wo3Start.AddMinutes((int)tireTask.EstimatedDurationInMins);
        var wo3 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[2].Id,
            wo3Start,
            wo3End,
            labors[2].Id,
            bays[2].Id,
            ToWorkOrderTasks([tireTask], invItems)).Value;
        seededOrders.Add(wo3);

        // 4. ??? ???? (Cancelled)
        var wo4Start = today.AddHours(15).AddMinutes(0);
        var wo4End = wo4Start.AddMinutes((int)batteryTask.EstimatedDurationInMins);
        var wo4 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[3].Id,
            wo4Start,
            wo4End,
            labors[3].Id,
            bays[3].Id,
            ToWorkOrderTasks([batteryTask], invItems)).Value;

        wo4.Cancel(_timeProvider);
        seededOrders.Add(wo4);

        // ==========================================
        // ????? ?????? (Tomorrow): ???? ????? ????
        // ==========================================

        // 5. ???? ???? (Scheduled)
        var wo5Start = tomorrow.AddHours(9).AddMinutes(0);
        var wo5End = wo5Start.AddMinutes((int)oilTask.EstimatedDurationInMins);
        var wo5 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[4].Id,
            wo5Start,
            wo5End,
            labors[0].Id,
            bays[0].Id,
            ToWorkOrderTasks([oilTask], invItems)).Value;
        seededOrders.Add(wo5);

        // 6. ???? ?????? ????? ??????? (Scheduled)
        var combinedTasks = new[] { brakeTask, batteryTask };
        var totalMins = combinedTasks.Sum(t => (int)t.EstimatedDurationInMins);
        var wo6Start = tomorrow.AddHours(10).AddMinutes(30);
        var wo6End = wo6Start.AddMinutes(totalMins);
        var wo6 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[5].Id,
            wo6Start,
            wo6End,
            labors[1].Id,
            bays[1].Id,
            ToWorkOrderTasks(combinedTasks, invItems)).Value;
        seededOrders.Add(wo6);

        // 7. ???? ????? ?????? (Scheduled)
        var wo7Start = tomorrow.AddHours(13).AddMinutes(30);
        var wo7End = wo7Start.AddMinutes((int)tireTask.EstimatedDurationInMins);
        var wo7 = WorkOrder.Create(
            Guid.CreateVersion7(),
            vehicles[6].Id,
            wo7Start,
            wo7End,
            labors[2].Id,
            bays[2].Id,
            ToWorkOrderTasks([tireTask], invItems)).Value;
        seededOrders.Add(wo7);

        _context.WorkOrders.AddRange(seededOrders);
        await _context.SaveChangesAsync();
    }

    private static List<WorkOrderTask> ToWorkOrderTasks(IEnumerable<RepairTask> repairTasks, Dictionary<Guid, InventoryItem> inventoryItemsMap)
    {
        return repairTasks.Select(rt =>
        {
            var parts = rt.Parts.Select(p =>
            {
                var invItem = inventoryItemsMap.GetValueOrDefault(p.InventoryItemId);
                return new WorkOrderTaskPart(p.InventoryItemId, invItem?.Name ?? "Spare Part", invItem?.Cost ?? 10m, p.Quantity);
            }).ToList();

            return new WorkOrderTask(
                id: Guid.CreateVersion7(),
                originalTaskId: rt.Id,
                name: rt.Name,
                laborCost: rt.LaborCost,
                estimatedDurationInMins: rt.EstimatedDurationInMins,
                parts: parts);
        }).ToList();
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