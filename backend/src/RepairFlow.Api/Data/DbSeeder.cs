using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Data;

/// <summary>
/// Демо-данные — на английском: витрина рассчитана на англоязычного заказчика. Интерфейс
/// переключается на русский, а содержимое заявок остаётся таким, каким его «написали», — как
/// в любой настоящей системе, где текст клиента никто не переводит.
///
/// Нужны они не для тестов, а для витрины: заказчик открывает демо и сразу видит
/// заполненный дашборд, историю переходов и сметы, а не пустые таблицы с надписью «нет данных».
/// </summary>
public static class DbSeeder
{
    public const string ManagerEmail = "manager@demo.io";
    public const string TechnicianEmail = "master@demo.io";
    public const string ClientEmail = "client@demo.io";

    public static string DemoEmailFor(UserRole role) => role switch
    {
        UserRole.Manager => ManagerEmail,
        UserRole.Technician => TechnicianEmail,
        _ => ClientEmail
    };

    public static async Task SeedAsync(
        AppDbContext db,
        DemoOptions options,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("The database already has data — seeding skipped.");
            return;
        }

        logger.LogInformation("Seeding demo data…");

        var hash = BCrypt.Net.BCrypt.HashPassword(options.Password);
        var random = new Random(20260819);
        var now = DateTime.UtcNow;

        var manager = NewUser(ManagerEmail, "Emily Novak", "+1 415 555 0110", UserRole.Manager, hash, now);

        var technicians = new[]
        {
            NewUser(TechnicianEmail, "Marcus Reed", "+1 415 555 0120", UserRole.Technician, hash, now),
            NewUser("tech2@demo.io", "Alex Moore", "+1 415 555 0121", UserRole.Technician, hash, now),
            NewUser("tech3@demo.io", "Sam Patel", "+1 415 555 0122", UserRole.Technician, hash, now)
        };

        var clients = new[]
        {
            NewUser(ClientEmail, "Daniel Carter", "+1 415 555 0130", UserRole.Client, hash, now),
            NewUser("client2@demo.io", "Anna Lee", "+1 415 555 0131", UserRole.Client, hash, now),
            NewUser("client3@demo.io", "Chris Walker", "+1 415 555 0132", UserRole.Client, hash, now),
            NewUser("client4@demo.io", "Olivia Brooks", "+1 415 555 0133", UserRole.Client, hash, now)
        };

        db.Users.Add(manager);
        db.Users.AddRange(technicians);
        db.Users.AddRange(clients);

        var targets = new[]
        {
            OrderStatus.New, OrderStatus.New, OrderStatus.New,
            OrderStatus.Diagnostics, OrderStatus.Diagnostics, OrderStatus.Diagnostics,
            OrderStatus.AwaitingEstimateApproval, OrderStatus.AwaitingEstimateApproval, OrderStatus.AwaitingEstimateApproval,
            OrderStatus.InProgress, OrderStatus.InProgress, OrderStatus.InProgress, OrderStatus.InProgress,
            OrderStatus.ReadyForPickup, OrderStatus.ReadyForPickup,
            OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Completed,
            OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Completed,
            OrderStatus.Cancelled, OrderStatus.ClientRejected
        };

        var sequence = 1;

        for (var index = 0; index < targets.Length; index++)
        {
            var target = targets[index];
            var device = Devices[index % Devices.Length];
            var client = clients[index % clients.Length];
            var technician = technicians[index % technicians.Length];

            // Заявки размазаны по последним двум месяцам, чтобы график на дашборде был живым.
            var createdAt = now.AddDays(-random.Next(1, 60)).AddHours(-random.Next(0, 12));

            var order = new Order
            {
                Number = OrderNumberGenerator.Format(createdAt.Year, sequence++),
                ClientId = client.Id,
                Client = client,
                DeviceType = device.Type,
                Brand = device.Brand,
                Model = device.Model,
                SerialNumber = $"SN{random.Next(100000, 999999)}",
                ProblemDescription = device.Problem,
                Status = OrderStatus.New,
                Priority = (OrderPriority)random.Next(0, 3),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            db.Orders.Add(order);
            db.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Order = order,
                FromStatus = null,
                ToStatus = OrderStatus.New,
                ChangedById = client.Id,
                ChangedBy = client,
                Comment = null,
                ChangedAt = createdAt
            });

            var path = PathTo(target);
            var stamp = createdAt;
            var previous = OrderStatus.New;

            foreach (var status in path)
            {
                stamp = stamp.AddHours(random.Next(3, 30));

                var author = status switch
                {
                    OrderStatus.InProgress or OrderStatus.ClientRejected => client,
                    OrderStatus.Cancelled => client,
                    _ => technician
                };

                db.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Order = order,
                    FromStatus = previous,
                    ToStatus = status,
                    ChangedById = author.Id,
                    ChangedBy = author,
                    Comment = CommentFor(status),
                    ChangedAt = stamp
                });

                previous = status;
            }

            order.Status = target;
            order.UpdatedAt = stamp;

            if (target != OrderStatus.New && target != OrderStatus.Cancelled)
            {
                order.AssignedTechnicianId = technician.Id;
                order.AssignedTechnician = technician;
            }

            // Смета появляется у всех, кто дошёл хотя бы до согласования.
            if (target is OrderStatus.AwaitingEstimateApproval or OrderStatus.InProgress
                or OrderStatus.ReadyForPickup or OrderStatus.Completed or OrderStatus.ClientRejected)
            {
                var items = BuildItems(order, device, random, stamp);
                db.OrderItems.AddRange(items);

                var total = EstimateCalculator.Total(items);
                order.EstimatedCost = total;

                if (target == OrderStatus.Completed)
                {
                    order.FinalCost = total;
                    order.CompletedAt = stamp;
                }
            }

            db.Comments.Add(new Comment
            {
                OrderId = order.Id,
                Order = order,
                AuthorId = client.Id,
                Author = client,
                Text = "Hi! Roughly how long will the repair take?",
                IsInternal = false,
                CreatedAt = createdAt.AddHours(2)
            });

            if (target != OrderStatus.New)
            {
                db.Comments.Add(new Comment
                {
                    OrderId = order.Id,
                    Order = order,
                    AuthorId = technician.Id,
                    Author = technician,
                    Text = "Picked it up. I’ll know more after diagnostics — usually a day or two.",
                    IsInternal = false,
                    CreatedAt = createdAt.AddHours(5)
                });

                db.Comments.Add(new Comment
                {
                    OrderId = order.Id,
                    Order = order,
                    AuthorId = technician.Id,
                    Author = technician,
                    Text = "Same defect as last week — the part is in stock.",
                    IsInternal = true,
                    CreatedAt = createdAt.AddHours(6)
                });
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Demo data ready: {Users} users, {Orders} requests. Every account uses the password {Password}",
            await db.Users.CountAsync(ct),
            await db.Orders.CountAsync(ct),
            options.Password);
    }

    private static User NewUser(
        string email,
        string fullName,
        string phone,
        UserRole role,
        string passwordHash,
        DateTime now) => new()
    {
        Email = email,
        FullName = fullName,
        Phone = phone,
        Role = role,
        PasswordHash = passwordHash,
        IsActive = true,
        CreatedAt = now.AddDays(-90)
    };

    private static IReadOnlyList<OrderStatus> PathTo(OrderStatus target) => target switch
    {
        OrderStatus.New => Array.Empty<OrderStatus>(),
        OrderStatus.Cancelled => new[] { OrderStatus.Cancelled },
        OrderStatus.Diagnostics => new[] { OrderStatus.Diagnostics },
        OrderStatus.AwaitingEstimateApproval => new[]
        {
            OrderStatus.Diagnostics, OrderStatus.AwaitingEstimateApproval
        },
        OrderStatus.ClientRejected => new[]
        {
            OrderStatus.Diagnostics, OrderStatus.AwaitingEstimateApproval, OrderStatus.ClientRejected
        },
        OrderStatus.InProgress => new[]
        {
            OrderStatus.Diagnostics, OrderStatus.AwaitingEstimateApproval, OrderStatus.InProgress
        },
        OrderStatus.ReadyForPickup => new[]
        {
            OrderStatus.Diagnostics, OrderStatus.AwaitingEstimateApproval,
            OrderStatus.InProgress, OrderStatus.ReadyForPickup
        },
        _ => new[]
        {
            OrderStatus.Diagnostics, OrderStatus.AwaitingEstimateApproval,
            OrderStatus.InProgress, OrderStatus.ReadyForPickup, OrderStatus.Completed
        }
    };

    private static string CommentFor(OrderStatus status) => status switch
    {
        OrderStatus.Diagnostics => "Taken in for diagnostics",
        OrderStatus.AwaitingEstimateApproval => "Estimate ready, sent to the customer",
        OrderStatus.InProgress => "Estimate approved by the customer",
        OrderStatus.ReadyForPickup => "Repair finished, device tested",
        OrderStatus.Completed => "Handed over to the customer",
        OrderStatus.Cancelled => "Cancelled by the customer",
        OrderStatus.ClientRejected => "The customer declined the repair",
        _ => string.Empty
    };

    private static List<OrderItem> BuildItems(Order order, DeviceTemplate device, Random random, DateTime createdAt)
    {
        var items = new List<OrderItem>
        {
            new()
            {
                OrderId = order.Id,
                Order = order,
                Type = OrderItemType.Labor,
                Name = "Diagnostics",
                Quantity = 1,
                UnitPrice = 45,
                CreatedAt = createdAt
            },
            new()
            {
                OrderId = order.Id,
                Order = order,
                Type = OrderItemType.Labor,
                Name = device.Work,
                Quantity = Math.Round((decimal)random.Next(10, 40) / 10m, 2),
                UnitPrice = 60,
                CreatedAt = createdAt
            }
        };

        if (device.Part is not null)
        {
            items.Add(new OrderItem
            {
                OrderId = order.Id,
                Order = order,
                Type = OrderItemType.Part,
                Name = device.Part,
                Quantity = 1,
                UnitPrice = random.Next(4, 52) * 5,
                CreatedAt = createdAt
            });
        }

        return items;
    }

    private sealed record DeviceTemplate(
        string Type,
        string Brand,
        string Model,
        string Problem,
        string Work,
        string? Part);

    private static readonly DeviceTemplate[] Devices =
    {
        new("Laptop", "Lenovo", "ThinkPad T14", "Won’t turn on after a coffee spill; there are signs of corrosion on the board.", "Board cleaning and recovery", "Keyboard ribbon cable"),
        new("Phone", "Apple", "iPhone 13", "Cracked screen; the touchscreen doesn’t respond in the lower third.", "Display module replacement", "Display module"),
        new("Laptop", "ASUS", "ZenBook 14", "The fan is very loud; it overheats and shuts down under load.", "Cooling system service, new thermal paste", "Cooling fan"),
        new("TV", "Samsung", "UE50TU7100", "Picture is fine, but there’s no sound from the speakers or over HDMI.", "Audio path repair", "Amplifier chip"),
        new("Phone", "Samsung", "Galaxy S21", "Battery drains completely in half a day.", "Battery replacement", "Battery"),
        new("Laptop", "HP", "Pavilion 15", "Won’t charge; the power LED stays off.", "Power circuit repair", "Power jack"),
        new("Tablet", "Apple", "iPad Air 4", "Part of the touchscreen stopped working after a drop.", "Glass replacement", "Touch panel"),
        new("Monitor", "LG", "27UP650", "Stripes across the screen when connected over DisplayPort.", "Panel and ribbon diagnostics", null),
        new("Printer", "Kyocera", "Ecosys P2040", "Jams paper on every other page.", "Feed assembly replacement", "Paper feed roller"),
        new("Phone", "Xiaomi", "Redmi Note 12", "Can’t hear the other person during calls.", "Earpiece speaker replacement", "Earpiece speaker"),
        new("Laptop", "Acer", "Aspire 5", "Several keys on the keyboard don’t work.", "Keyboard replacement", "Keyboard"),
        new("Game console", "Sony", "PlayStation 5", "Overheats and reboots during games.", "Cooling system service", "Thermal pads")
    };
}
