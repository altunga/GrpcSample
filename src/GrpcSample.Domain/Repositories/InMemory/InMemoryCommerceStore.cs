using System.Collections.Concurrent;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Audit;
using GrpcSample.Domain.Models.Blocks;
using GrpcSample.Domain.Models.Chat;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Seed;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryCommerceStore
{
    public Lock ProductLock { get; } = new();
    public Lock CustomerLock { get; } = new();

    public ConcurrentDictionary<string, Product> Products { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, Customer> Customers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, Order> Orders { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, BlockArtifact> Blocks { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentQueue<TelemetryReading> Telemetry { get; } = new();
    public ConcurrentQueue<ChatRoomMessage> ChatMessages { get; } = new();
    public ConcurrentQueue<AuditEntry> AuditEntries { get; } = new();

    private int _nextProductSeq = 61;
    private int _nextOrderSeq = 1036;
    private int _nextBlockSeq = 17;
    private int _nextTelemetrySeq = 121;
    private int _totalAuditCount;

    public string NextProductId() => $"PRD-{Interlocked.Increment(ref _nextProductSeq) - 1:000}";
    public string NextOrderId() => $"ORD-{Interlocked.Increment(ref _nextOrderSeq) - 1}";
    public string NextBlockId() => $"BLK-{Interlocked.Increment(ref _nextBlockSeq) - 1:000}";
    public string NextTelemetryId() => $"TEL-{Interlocked.Increment(ref _nextTelemetrySeq) - 1:0000}";
    public int IncrementAuditCount() => Interlocked.Increment(ref _totalAuditCount);
    public int TotalAuditCount => Volatile.Read(ref _totalAuditCount);

    public static InMemoryCommerceStore CreateSeeded()
    {
        var store = new InMemoryCommerceStore();

        var products = CommerceSeedData.CreateProducts();
        foreach (var p in products)
            store.Products[p.Id] = p;

        var customers = CommerceSeedData.CreateCustomers();
        foreach (var c in customers)
            store.Customers[c.Id] = c;

        var orders = CommerceSeedData.CreateOrders(customers, products);
        foreach (var o in orders)
            store.Orders[o.Id] = o;

        foreach (var t in CommerceSeedData.CreateTelemetryReadings())
            store.Telemetry.Enqueue(t);

        foreach (var b in CommerceSeedData.CreateBlockArtifacts())
            store.Blocks[b.BlockId] = b;

        foreach (var m in CommerceSeedData.CreateChatMessages())
            store.ChatMessages.Enqueue(m);

        return store;
    }
}
