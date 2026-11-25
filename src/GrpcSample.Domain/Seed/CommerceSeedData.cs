using System.Security.Cryptography;
using System.Text;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Blocks;
using GrpcSample.Domain.Models.Chat;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Models.Telemetry;

namespace GrpcSample.Domain.Seed;

public static class CommerceSeedData
{
    public static readonly DateTime BaseTimestampUtc = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<Product> CreateProducts()
    {
        var specs = new (ProductCategory Cat, string Prefix, string Brand, string[] Names, decimal BasePrice)[]
        {
            (ProductCategory.Electronics, "ELEC", "VoltCore", [
                "UltraBook Pro 16", "4K HDR Monitor 32\"", "Thunderbolt 5 Dock", "NVMe Gen5 SSD 4TB",
                "Wi-Fi 7 Tri-Band Router", "Mini Workstation PC", "SmartUPS 1500VA", "PCIe Capture Card 4K",
                "GaN 140W Charger", "Biometric Security Key"
            ], 199.90m),
            (ProductCategory.Books, "BOOK", "TechPress", [
                "gRPC & Distributed Systems", "High-Performance .NET 11", "Protobuf Wire Internals", "Designing Event Streams",
                "Zero-Allocation C#", "Cloud Native Observability", "Modern Cryptography Guide", "Kestrel & HTTP/3 Deep Dive",
                "Domain Modeling in Practice", "Resilient Microservices"
            ], 34.50m),
            (ProductCategory.HomeOffice, "HOME", "ErgoCraft", [
                "Dual-Motor Standing Desk", "Mesh Ergonomic Chair", "Monitor Arm Dual Pro", "LED Task Light Bar",
                "Acoustic Desk Divider", "Under-Desk Cable Tray", "Bamboo Laptop Riser", "Footrest Cushion Pro",
                "Magnetic Whiteboard 120cm", "Desk Mat Wool Felt"
            ], 79.00m),
            (ProductCategory.Gaming, "GAME", "ApexGear", [
                "Hall-Effect Mechanical Keyboard", "8K Wireless Gaming Mouse", "OLED Curved Gaming Monitor", "Pro Racing Wheel Base",
                "Low-Latency Wireless Controller", "RGB Glass Mousepad", "Flight Stick HOTAS", "Stream Deck Macro Pad",
                "240Hz Esports Display", "Cooling Pad Turbo"
            ], 89.99m),
            (ProductCategory.Audio, "AUDI", "SonicLab", [
                "Studio Reference Headphones", "USB-C Condenser Mic", "2x2 Audio Interface", "Active Studio Monitors",
                "ANC Wireless Over-Ear", "Boom Arm Low-Profile", "DAC & Balanced Amp", "Acoustic Foam 12-Pack",
                "Lavalier Wireless Kit", "Portable Field Recorder"
            ], 119.00m),
            (ProductCategory.Accessories, "ACCS", "LinkPro", [
                "Braided USB4 Cable 2m", "HDMI 2.1 Ultra Cable", "Aluminum USB-C Hub", "RFID Travel Sleeve",
                "Laptop Sleeve 16\" Kevlar", "Precision Screwdriver Set", "Thermal Paste Extreme", "Microfiber Cleaning Kit",
                "Magnetic Cable Clips 6x", "DisplayPort 2.1 Cable"
            ], 14.90m)
        };

        var products = new List<Product>(60);
        int globalIndex = 1;

        foreach (var (cat, prefix, brand, names, basePrice) in specs)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string id = $"PRD-{globalIndex:000}";
                string sku = $"SKU-{prefix}-{i + 1:000}";
                decimal price = Math.Round(basePrice + (i * (basePrice * 0.22m)), 2);
                int stock = 12 + ((globalIndex * 7) % 210);
                double rating = Math.Round(3.9 + ((globalIndex % 11) * 0.1), 1);

                products.Add(new Product
                {
                    Id = id,
                    Sku = sku,
                    Name = names[i],
                    Description = $"{brand} {names[i]} — enterprise-ready {cat} item #{globalIndex} built for modern workflows.",
                    Category = cat,
                    Price = price,
                    StockQuantity = stock,
                    Rating = rating,
                    Tags = [cat.ToString().ToLowerInvariant(), brand.ToLowerInvariant(), i % 2 == 0 ? "bestseller" : "featured"],
                    Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["brand"] = brand,
                        ["warranty"] = cat == ProductCategory.Books ? "none" : "24-months",
                        ["origin"] = (globalIndex % 3) switch { 0 => "DE", 1 => "JP", _ => "TR" },
                        ["weight_g"] = ((globalIndex * 45) % 3500 + 120).ToString()
                    },
                    IsActive = globalIndex != 59, // 59 active, PRD-059 inactive for filter tests
                    CreatedAtUtc = BaseTimestampUtc.AddDays(globalIndex)
                });
                globalIndex++;
            }
        }

        return products;
    }

    public static IReadOnlyList<Customer> CreateCustomers()
    {
        var locations = new (string Country, string City)[]
        {
            ("TR", "Istanbul"), ("TR", "Ankara"), ("TR", "Izmir"),
            ("DE", "Berlin"), ("GB", "London")
        };

        var firstNames = new[] { "Eren", "Deniz", "Selin", "Kaan", "Zeynep", "Emre", "Defne", "Mert", "Elif", "Can" };
        var lastNames = new[] { "Yilmaz", "Kaya", "Demir", "Sahin", "Celik", "Aydin", "Ozturk", "Arslan" };

        var customers = new List<Customer>(25);
        for (int i = 1; i <= 25; i++)
        {
            var first = firstNames[(i - 1) % firstNames.Length];
            var last = lastNames[(i - 1) % lastNames.Length];
            var (country, city) = locations[(i - 1) % locations.Length];
            var tier = (CustomerTier)((i - 1) % 4);

            customers.Add(new Customer
            {
                Id = $"CUS-{i:000}",
                FullName = $"{first} {last}",
                Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}{i}@example.dev",
                Tier = tier,
                Country = country,
                City = city,
                TotalSpent = Math.Round(250m * i * ((int)tier + 1), 2),
                CreatedAtUtc = BaseTimestampUtc.AddDays(i * 2)
            });
        }

        return customers;
    }

    public static IReadOnlyList<Order> CreateOrders(IReadOnlyList<Customer> customers, IReadOnlyList<Product> products)
    {
        var orders = new List<Order>(35);
        for (int i = 1; i <= 35; i++)
        {
            var customer = customers[(i - 1) % customers.Count];
            var p1 = products[(i * 2 - 1) % products.Count];
            var p2 = products[(i * 3) % products.Count];

            var items = new List<OrderLineItem>
            {
                new()
                {
                    ProductId = p1.Id,
                    Sku = p1.Sku,
                    ProductName = p1.Name,
                    Quantity = 1 + (i % 3),
                    UnitPrice = p1.Price
                },
                new()
                {
                    ProductId = p2.Id,
                    Sku = p2.Sku,
                    ProductName = p2.Name,
                    Quantity = 1 + (i % 2),
                    UnitPrice = p2.Price
                }
            };

            var status = (OrderStatus)((i - 1) % 5);
            orders.Add(new Order
            {
                Id = $"ORD-{1000 + i}",
                CustomerId = customer.Id,
                Status = status,
                Items = items,
                TotalAmount = items.Sum(x => x.UnitPrice * x.Quantity),
                ShippingAddress = $"{100 + i} Tech Avenue, {customer.City}, {customer.Country}",
                TrackingNumber = status is OrderStatus.Shipped or OrderStatus.Delivered ? $"TRK-{900000 + i}" : null,
                CreatedAtUtc = BaseTimestampUtc.AddHours(i * 12)
            });
        }

        return orders;
    }

    public static IReadOnlyList<TelemetryReading> CreateTelemetryReadings()
    {
        var warehouses = new[] { "WH-IST-01", "WH-ANK-02", "WH-IZM-03", "WH-BER-04", "WH-LON-05" };
        var readings = new List<TelemetryReading>(120);

        for (int i = 1; i <= 120; i++)
        {
            string warehouse = warehouses[(i - 1) % warehouses.Length];
            bool isAnomaly = i % 6 == 0; // 20 deterministic anomalies across warehouses (4 per warehouse)
            double temp = isAnomaly ? 34.5 + (i % 5) : 20.5 + (i % 7) * 0.6;
            double humidity = 42.0 + (i % 15);
            double cpu = isAnomaly ? 88.5 + (i % 9) : 28.0 + (i % 35);

            readings.Add(new TelemetryReading
            {
                ReadingId = $"TEL-{i:0000}",
                WarehouseId = warehouse,
                SensorId = $"SENS-{(i % 4) + 1:00}",
                TemperatureCelsius = Math.Round(temp, 2),
                HumidityPercent = Math.Round(humidity, 2),
                CpuLoadPercent = Math.Round(cpu, 2),
                IsAnomaly = isAnomaly,
                RecordedAtUtc = BaseTimestampUtc.AddMinutes(i * 5)
            });
        }

        return readings;
    }

    public static IReadOnlyList<BlockArtifact> CreateBlockArtifacts()
    {
        var blocks = new List<BlockArtifact>(16);
        for (int i = 1; i <= 16; i++)
        {
            int size = 4096 * ((i % 4) + 1); // 4KB to 16KB deterministic payloads
            var payload = new byte[size];
            var header = Encoding.UTF8.GetBytes($"GRPC-BLOCK-{i:000}|SEED|SIZE={size}|");
            Array.Copy(header, payload, header.Length);
            for (int b = header.Length; b < payload.Length; b++)
            {
                payload[b] = (byte)((b * 31 + i * 17) & 0xFF);
            }

            string sha = Convert.ToHexStringLower(SHA256.HashData(payload));
            blocks.Add(new BlockArtifact
            {
                BlockId = $"BLK-{i:000}",
                BlockName = i == 1 ? "firmware-v11.bin" : $"block-archive-{i:000}.bin",
                ContentType = "application/octet-stream",
                Data = payload,
                TotalBytes = payload.Length,
                ChunkCount = (int)Math.Ceiling(payload.Length / 4096.0),
                Sha256Hex = sha,
                UploadedAtUtc = BaseTimestampUtc.AddHours(i)
            });
        }

        return blocks;
    }

    public static IReadOnlyList<ChatRoomMessage> CreateChatMessages() =>
    [
        new() { MessageId = "MSG-001", RoomId = "order-support", Sender = "system", Content = "Order support channel initialized.", SentAtUtc = BaseTimestampUtc },
        new() { MessageId = "MSG-002", RoomId = "order-support", Sender = "operator@grpcsample.dev", Content = "ORD-1001 is queued for express dispatch.", SentAtUtc = BaseTimestampUtc.AddMinutes(5) },
        new() { MessageId = "MSG-003", RoomId = "warehouse-ops", Sender = "system", Content = "Telemetry alerts connected to warehouse-ops.", SentAtUtc = BaseTimestampUtc.AddMinutes(10) },
        new() { MessageId = "MSG-004", RoomId = "vip-desk", Sender = "admin@grpcsample.dev", Content = "Enterprise SLA tier active.", SentAtUtc = BaseTimestampUtc.AddMinutes(15) }
    ];
}
