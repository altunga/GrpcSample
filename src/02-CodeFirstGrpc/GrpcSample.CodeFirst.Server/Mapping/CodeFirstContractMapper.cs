using GrpcSample.CodeFirst.Contracts;
using GrpcSample.CodeFirst.Contracts.Models.Blocks;
using GrpcSample.CodeFirst.Contracts.Models.Chat;
using GrpcSample.CodeFirst.Contracts.Models.Orders;
using GrpcSample.CodeFirst.Contracts.Models.Products;
using GrpcSample.CodeFirst.Contracts.Models.Telemetry;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Catalog;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Pricing;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Contracts;

namespace GrpcSample.CodeFirst.Server.Mapping;

public static class CodeFirstContractMapper
{
    public static ProductModel ToModel(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Description = p.Description,
        Category = p.Category,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        Rating = p.Rating,
        Tags = [.. p.Tags],
        Attributes = new Dictionary<string, string>(p.Attributes, StringComparer.OrdinalIgnoreCase),
        IsActive = p.IsActive,
        CreatedAtUtc = DateTime.SpecifyKind(p.CreatedAtUtc, DateTimeKind.Utc)
    };

    public static OrderModel ToModel(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        Status = o.Status,
        Items = o.Items.Select(i => new OrderLineItemModel
        {
            ProductId = i.ProductId,
            Sku = i.Sku,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        TotalAmount = o.TotalAmount,
        ShippingAddress = o.ShippingAddress,
        TrackingNumber = o.TrackingNumber,
        CreatedAtUtc = DateTime.SpecifyKind(o.CreatedAtUtc, DateTimeKind.Utc)
    };

    public static CatalogSummaryModel ToModel(CatalogSummary s) => new()
    {
        TotalProducts = s.TotalProducts,
        ActiveProducts = s.ActiveProducts,
        TotalStockUnits = s.TotalStockUnits,
        TotalInventoryValue = s.TotalInventoryValue,
        TotalCustomers = s.TotalCustomers,
        TotalOrders = s.TotalOrders,
        TotalRevenue = s.TotalRevenue,
        TotalTelemetryReadings = s.TotalTelemetryReadings,
        TotalBlocks = s.TotalBlocks,
        ProductsByCategory = new Dictionary<string, int>(s.ProductsByCategory, StringComparer.OrdinalIgnoreCase)
    };

    public static TelemetryReadingModel ToModel(TelemetryReading t) => new()
    {
        ReadingId = t.ReadingId,
        WarehouseId = t.WarehouseId,
        SensorId = t.SensorId,
        TemperatureCelsius = t.TemperatureCelsius,
        HumidityPercent = t.HumidityPercent,
        CpuLoadPercent = t.CpuLoadPercent,
        IsAnomaly = t.IsAnomaly,
        RecordedAtUtc = DateTime.SpecifyKind(t.RecordedAtUtc, DateTimeKind.Utc)
    };

    public static TelemetryReading ToDomain(TelemetryReadingModel model) => new()
    {
        ReadingId = model.ReadingId,
        WarehouseId = model.WarehouseId,
        SensorId = model.SensorId,
        TemperatureCelsius = model.TemperatureCelsius,
        HumidityPercent = model.HumidityPercent,
        CpuLoadPercent = model.CpuLoadPercent,
        IsAnomaly = model.IsAnomaly,
        RecordedAtUtc = model.RecordedAtUtc
    };

    public static BlockDownloadChunk ToModel(BlockChunkSlice slice) => new()
    {
        BlockId = slice.BlockId,
        BlockName = slice.BlockName,
        Sequence = slice.Sequence,
        IsLast = slice.IsLast,
        ChunkData = slice.Data.ToArray(),
        ExpectedSha256Hex = slice.ExpectedSha256Hex
    };

    public static ChatCommandInput ToDomain(ChatStreamRequest req) => new(
        RoomId: req.RoomId,
        Sender: req.Sender,
        Command: (ChatCommandKind)req.Command,
        Text: req.Text,
        OrderId: req.OrderId);

    public static ChatStreamResponse ToModel(ChatCommandReply reply) => new()
    {
        MessageId = reply.MessageId,
        RoomId = reply.RoomId,
        Sender = reply.Sender,
        Text = reply.Text,
        IsSystem = reply.IsSystem,
        SentAtUtc = DateTime.SpecifyKind(reply.SentAtUtc, DateTimeKind.Utc)
    };

    public static PriceQuoteResponse ToModel(QuoteResult quote) => new()
    {
        Sku = quote.Sku,
        ProductName = quote.ProductName,
        RequestedQuantity = quote.RequestedQuantity,
        UnitPrice = quote.UnitPrice,
        DiscountPercent = quote.DiscountPercent,
        LineTotal = quote.LineTotal,
        RunningSubtotal = quote.RunningSubtotal,
        InStock = quote.InStock,
        Note = quote.Note
    };
}
