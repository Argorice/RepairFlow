using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Data;

/// <summary>
/// Демо-данные. Нужны не для тестов, а для витрины: заказчик открывает демо и сразу видит
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
            logger.LogInformation("База уже содержит данные — сидер пропущен.");
            return;
        }

        logger.LogInformation("Заполняю базу демо-данными…");

        var hash = BCrypt.Net.BCrypt.HashPassword(options.Password);
        var random = new Random(20260819);
        var now = DateTime.UtcNow;

        var manager = NewUser(ManagerEmail, "Ирина Соколова", "+7 900 100-10-10", UserRole.Manager, hash, now);

        var technicians = new[]
        {
            NewUser(TechnicianEmail, "Павел Кузнецов", "+7 900 200-20-20", UserRole.Technician, hash, now),
            NewUser("tech2@demo.io", "Алексей Морозов", "+7 900 200-20-21", UserRole.Technician, hash, now),
            NewUser("tech3@demo.io", "Марат Ильясов", "+7 900 200-20-22", UserRole.Technician, hash, now)
        };

        var clients = new[]
        {
            NewUser(ClientEmail, "Дмитрий Орлов", "+7 900 300-30-30", UserRole.Client, hash, now),
            NewUser("client2@demo.io", "Анна Лебедева", "+7 900 300-30-31", UserRole.Client, hash, now),
            NewUser("client3@demo.io", "Сергей Титов", "+7 900 300-30-32", UserRole.Client, hash, now),
            NewUser("client4@demo.io", "Ольга Белова", "+7 900 300-30-33", UserRole.Client, hash, now)
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
                Comment = "Заявка создана",
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
                Text = "Подскажите, пожалуйста, ориентировочные сроки.",
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
                    Text = "Взял в работу, после диагностики сообщу точнее.",
                    IsInternal = false,
                    CreatedAt = createdAt.AddHours(5)
                });

                db.Comments.Add(new Comment
                {
                    OrderId = order.Id,
                    Order = order,
                    AuthorId = technician.Id,
                    Author = technician,
                    Text = "Внутренняя заметка: аналогичный дефект был на прошлой неделе, деталь есть на складе.",
                    IsInternal = true,
                    CreatedAt = createdAt.AddHours(6)
                });
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Демо-данные готовы: {Users} пользователей, {Orders} заявок. Пароль всех аккаунтов — {Password}",
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
        OrderStatus.Diagnostics => "Принято в диагностику",
        OrderStatus.AwaitingEstimateApproval => "Смета сформирована, отправлена клиенту",
        OrderStatus.InProgress => "Смета подтверждена клиентом",
        OrderStatus.ReadyForPickup => "Ремонт завершён, аппарат протестирован",
        OrderStatus.Completed => "Выдано клиенту",
        OrderStatus.Cancelled => "Заявка отменена клиентом",
        OrderStatus.ClientRejected => "Клиент отказался от ремонта",
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
                Name = "Диагностика",
                Quantity = 1,
                UnitPrice = 1500,
                CreatedAt = createdAt
            },
            new()
            {
                OrderId = order.Id,
                Order = order,
                Type = OrderItemType.Labor,
                Name = device.Work,
                Quantity = Math.Round((decimal)random.Next(10, 40) / 10m, 2),
                UnitPrice = 2200,
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
                UnitPrice = random.Next(15, 190) * 100,
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
        new("Ноутбук", "Lenovo", "ThinkPad T14", "Не включается после попадания воды, есть следы окисления.", "Чистка и восстановление платы", "Клавиатурный шлейф"),
        new("Смартфон", "Apple", "iPhone 13", "Разбит экран, тачскрин частично не реагирует.", "Замена дисплейного модуля", "Дисплейный модуль"),
        new("Ноутбук", "ASUS", "ZenBook 14", "Сильно шумит вентилятор, греется и выключается под нагрузкой.", "Чистка системы охлаждения, замена термопасты", "Вентилятор охлаждения"),
        new("Телевизор", "Samsung", "UE50TU7100", "Изображение есть, звука нет ни через динамики, ни через HDMI.", "Ремонт аудиотракта", "Микросхема усилителя"),
        new("Смартфон", "Samsung", "Galaxy S21", "Быстро разряжается, за полдня уходит весь заряд.", "Замена аккумулятора", "Аккумулятор"),
        new("Ноутбук", "HP", "Pavilion 15", "Не заряжается, индикатор питания не горит.", "Ремонт цепи питания", "Разъём питания"),
        new("Планшет", "Apple", "iPad Air 4", "После падения не работает часть тачскрина.", "Замена стекла", "Тачскрин"),
        new("Монитор", "LG", "27UP650", "Полосы по экрану при подключении по DisplayPort.", "Диагностика матрицы и шлейфов", null),
        new("Принтер", "Kyocera", "Ecosys P2040", "Зажёвывает бумагу на каждой второй странице.", "Замена узла подачи", "Ролик подачи бумаги"),
        new("Смартфон", "Xiaomi", "Redmi Note 12", "Не слышно собеседника при разговоре.", "Замена разговорного динамика", "Динамик"),
        new("Ноутбук", "Acer", "Aspire 5", "Не работает часть клавиш на клавиатуре.", "Замена клавиатуры", "Клавиатура"),
        new("Игровая консоль", "Sony", "PlayStation 5", "Перегревается и уходит в перезагрузку в играх.", "Обслуживание системы охлаждения", "Термопрокладки")
    };
}
