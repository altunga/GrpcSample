using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Catalog;
using GrpcSample.Domain.Models.Pricing;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Contracts;
using GrpcSample.Native.Contracts;
using DomainOrder = GrpcSample.Domain.Models.Orders.Order;
using DomainProduct = GrpcSample.Domain.Models.Products.Product;
using DomainProductCategory = GrpcSample.Domain.Models.Products.ProductCategory;
using DomainCustomerTier = GrpcSample.Domain.Models.Customers.CustomerTier;
using ProtoProductCategory = GrpcSample.Native.Contracts.ProductCategory;
using ProtoOrderStatus = GrpcSample.Native.Contracts.OrderStatus;

namespace GrpcSample.Native.Server.Mapping;

public static class NativeContractMapper
{
    public static ProductModel ToProto(DomainProduct p)
    {
        var model = new ProductModel
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            Category = (ProtoProductCategory)p.Category,
            Price = (double)p.Price,
            StockQuantity = p.StockQuantity,
            Rating = p.Rating,
            IsActive = p.IsActive,
            CreatedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(p.CreatedAtUtc, DateTimeKind.Utc))
        };
        model.Tags.AddRange(p.Tags);
        foreach (var kvp in p.Attributes)
            model.Attributes[kvp.Key] = kvp.Value;
        return model;
    }

    public static OrderModel ToProto(DomainOrder o)
    {
        var model = new OrderModel
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            Status = (ProtoOrderStatus)o.Status,
            TotalAmount = (double)o.TotalAmount,
            ShippingAddress = o.ShippingAddress,
            CreatedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(o.CreatedAtUtc, DateTimeKind.Utc))
        };
        if (o.TrackingNumber is not null)
            model.TrackingNumber = o.TrackingNumber;

        model.Items.AddRange(o.Items.Select(i => new OrderLineItemModel
        {
            ProductId = i.ProductId,
            Sku = i.Sku,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = (double)i.UnitPrice
        }));
        return model;
    }

    public static CatalogSummaryModel ToProto(CatalogSummary s)
    {
        var model = new CatalogSummaryModel
        {
            TotalProducts = s.TotalProducts,
            ActiveProducts = s.ActiveProducts,
            TotalStockUnits = s.TotalStockUnits,
            TotalInventoryValue = (double)s.TotalInventoryValue,
            TotalCustomers = s.TotalCustomers,
            TotalOrders = s.TotalOrders,
            TotalRevenue = (double)s.TotalRevenue,
            TotalTelemetryReadings = s.TotalTelemetryReadings,
            TotalBlocks = s.TotalBlocks
        };
        foreach (var kvp in s.ProductsByCategory)
            model.ProductsByCategory[kvp.Key] = kvp.Value;
        return model;
    }

    public static TelemetryReadingModel ToProto(TelemetryReading t) => new()
    {
        ReadingId = t.ReadingId,
        WarehouseId = t.WarehouseId,
        SensorId = t.SensorId,
        TemperatureCelsius = t.TemperatureCelsius,
        HumidityPercent = t.HumidityPercent,
        CpuLoadPercent = t.CpuLoadPercent,
        IsAnomaly = t.IsAnomaly,
        RecordedAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(t.RecordedAtUtc, DateTimeKind.Utc))
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
        RecordedAtUtc = model.RecordedAtUtc?.ToDateTime() ?? default
    };

    public static BlockDownloadChunk ToProto(BlockChunkSlice slice) => new()
    {
        BlockId = slice.BlockId,
        BlockName = slice.BlockName,
        Sequence = slice.Sequence,
        IsLast = slice.IsLast,
        ChunkData = UnsafeByteOperations.UnsafeWrap(slice.Data),
        ExpectedSha256Hex = slice.ExpectedSha256Hex
    };

    public static ChatCommandInput ToDomain(ChatStreamRequest req) => new(
        RoomId: req.RoomId,
        Sender: req.Sender,
        Command: (ChatCommandKind)req.Command,
        Text: req.Text,
        OrderId: req.HasOrderId ? req.OrderId : null);

    public static ChatStreamResponse ToProto(ChatCommandReply reply) => new()
    {
        MessageId = reply.MessageId,
        RoomId = reply.RoomId,
        Sender = reply.Sender,
        Text = reply.Text,
        IsSystem = reply.IsSystem,
        SentAtUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(reply.SentAtUtc, DateTimeKind.Utc))
    };

    public static PriceQuoteResponse ToProto(QuoteResult quote) => new()
    {
        Sku = quote.Sku,
        ProductName = quote.ProductName,
        RequestedQuantity = quote.RequestedQuantity,
        UnitPrice = (double)quote.UnitPrice,
        DiscountPercent = (double)quote.DiscountPercent,
        LineTotal = (double)quote.LineTotal,
        RunningSubtotal = (double)quote.RunningSubtotal,
        InStock = quote.InStock,
        Note = quote.Note
    };

    public static ProductFilter ToDomain(ListProductsRequest req) => new(
        Category: req.HasCategory ? (DomainProductCategory)req.Category : null,
        SearchTerm: req.HasSearchTerm ? req.SearchTerm : null,
        ActiveOnly: req.ActiveOnly,
        MaxPrice: req.HasMaxPrice ? (decimal)req.MaxPrice : null,
        Page: req.Page <= 0 ? 1 : req.Page,
        PageSize: req.PageSize <= 0 ? 20 : req.PageSize);

    public static DomainCustomerTier ToDomain(Contracts.CustomerTier tier) => (DomainCustomerTier)tier;
}
