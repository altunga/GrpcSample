using System.Security.Claims;
using Grpc.Core;
using GrpcSample.CodeFirst.Contracts;
using GrpcSample.CodeFirst.Contracts.Models.Audit;
using GrpcSample.CodeFirst.Contracts.Models.Blocks;
using GrpcSample.CodeFirst.Contracts.Models.Chat;
using GrpcSample.CodeFirst.Contracts.Models.Common;
using GrpcSample.CodeFirst.Contracts.Models.Orders;
using GrpcSample.CodeFirst.Contracts.Models.Products;
using GrpcSample.CodeFirst.Contracts.Models.Telemetry;
using GrpcSample.CodeFirst.Contracts.Services;
using GrpcSample.CodeFirst.Server.Mapping;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;

namespace GrpcSample.CodeFirst.Server.Services;

public sealed class CommerceCodeFirstService(
    IProductCatalogService productCatalog,
    IOrderProcessingService orderProcessing,
    ITelemetryService telemetryService,
    IBlockStorageService blockStorage,
    IOrderChatProcessor chatProcessor,
    IAuditService auditService) : ICommerceCodeFirstService
{
    public ValueTask<ProductModel> GetProductAsync(IdRequest request, CallContext context = default) =>
        ValueTask.FromResult(CodeFirstContractMapper.ToModel(productCatalog.GetProduct(request.Id)));

    public ValueTask<ListProductsResponse> ListProductsAsync(ListProductsRequest request, CallContext context = default)
    {
        var paged = productCatalog.ListProducts(new ProductFilter(
            Category: request.Category,
            SearchTerm: request.SearchTerm,
            ActiveOnly: request.ActiveOnly,
            MaxPrice: request.MaxPrice,
            Page: request.Page,
            PageSize: request.PageSize));

        return ValueTask.FromResult(new ListProductsResponse
        {
            Items = paged.Items.Select(CodeFirstContractMapper.ToModel).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        });
    }

    [Authorize]
    public ValueTask<ProductModel> CreateProductAsync(CreateProductRequest request, CallContext context = default)
    {
        var created = productCatalog.CreateProduct(new CreateProductInput(
            Sku: request.Sku,
            Name: request.Name,
            Description: request.Description,
            Category: request.Category,
            Price: request.Price,
            StockQuantity: request.StockQuantity,
            Rating: request.Rating,
            Tags: request.Tags,
            Attributes: request.Attributes));

        return ValueTask.FromResult(CodeFirstContractMapper.ToModel(created));
    }

    [Authorize]
    public ValueTask<ProductModel> UpdateProductAsync(UpdateProductRequest request, CallContext context = default)
    {
        var updated = productCatalog.UpdateProduct(new UpdateProductInput(
            Id: request.Id,
            Name: request.Name,
            Description: request.Description,
            Category: request.Category,
            Price: request.Price,
            StockQuantity: request.StockQuantity,
            IsActive: request.IsActive,
            Tags: request.Tags,
            Attributes: request.Attributes));

        return ValueTask.FromResult(CodeFirstContractMapper.ToModel(updated));
    }

    [Authorize(Roles = "Admin")]
    public ValueTask<DeleteResponse> DeleteProductAsync(IdRequest request, CallContext context = default) =>
        ValueTask.FromResult(new DeleteResponse
        {
            Deleted = productCatalog.DeleteProduct(request.Id),
            Id = request.Id
        });

    public ValueTask<OrderModel> GetOrderAsync(IdRequest request, CallContext context = default) =>
        ValueTask.FromResult(CodeFirstContractMapper.ToModel(orderProcessing.GetOrder(request.Id)));

    public ValueTask<ListOrdersResponse> ListOrdersByCustomerAsync(IdRequest request, CallContext context = default) =>
        ValueTask.FromResult(new ListOrdersResponse
        {
            Items = orderProcessing.ListOrdersByCustomer(request.Id).Select(CodeFirstContractMapper.ToModel).ToList()
        });

    [Authorize]
    public ValueTask<OrderModel> CreateOrderAsync(CreateOrderRequest request, CallContext context = default)
    {
        var created = orderProcessing.CreateOrder(new CreateOrderInput(
            CustomerId: request.CustomerId,
            ShippingAddress: request.ShippingAddress,
            Items: request.Items.Select(i => new CreateOrderItemInput(i.ProductId, i.Quantity)).ToList()));

        return ValueTask.FromResult(CodeFirstContractMapper.ToModel(created));
    }

    public ValueTask<CatalogSummaryModel> GetCatalogSummaryAsync(Empty request, CallContext context = default) =>
        ValueTask.FromResult(CodeFirstContractMapper.ToModel(productCatalog.GetCatalogSummary()));

    [Authorize(Roles = "Admin")]
    public ValueTask<AdminAuditInfoResponse> GetAdminAuditInfoAsync(Empty request, CallContext context = default)
    {
        var user = context.ServerCallContext?.GetHttpContext().User;
        var stats = auditService.GetStats();
        return ValueTask.FromResult(new AdminAuditInfoResponse
        {
            TotalRequests = stats.TotalRequests,
            AuthenticatedUser = user?.Identity?.Name ?? "unknown",
            Role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "unknown",
            RecentEntries = stats.RecentEntries.Select(e => new AuditEntryModel
            {
                Engine = e.Engine,
                Method = e.Method,
                CorrelationId = e.CorrelationId,
                User = e.User ?? "anonymous",
                ElapsedMs = e.ElapsedMs,
                Status = e.Status,
                TimestampUtc = DateTime.SpecifyKind(e.TimestampUtc, DateTimeKind.Utc)
            }).ToList()
        });
    }

    public async ValueTask<SlowOperationResponse> SimulateSlowOperationAsync(
        SlowOperationRequest request,
        CallContext context = default)
    {
        var delay = Math.Clamp(request.DelayMs, 0, 30_000);
        if (delay > 0)
            await Task.Delay(delay, context.CancellationToken);

        return new SlowOperationResponse
        {
            Label = string.IsNullOrWhiteSpace(request.Label) ? "slow-op" : request.Label,
            ActualDelayMs = delay,
            ServerEngine = "protobuf-net-grpc"
        };
    }

    public async IAsyncEnumerable<ProductModel> StreamProductsAsync(
        StreamProductsRequest request,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        var items = productCatalog.StreamProducts(new ProductFilter(
            Category: request.Category,
            ActiveOnly: request.ActiveOnly));

        int count = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (request.DelayMs > 0)
                await Task.Delay(Math.Min(request.DelayMs, 1000), ct);

            count++;
            yield return CodeFirstContractMapper.ToModel(item);
        }

        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
    }

    public async IAsyncEnumerable<TelemetryReadingModel> SubscribeWarehouseTelemetryAsync(
        TelemetrySubscriptionRequest request,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        var readings = telemetryService.GetWarehouseTelemetry(request.WarehouseId, request.AnomaliesOnly, request.MaxReadings);

        int count = 0;
        foreach (var reading in readings)
        {
            ct.ThrowIfCancellationRequested();
            if (request.IntervalMs > 0)
                await Task.Delay(Math.Min(request.IntervalMs, 1000), ct);

            count++;
            yield return CodeFirstContractMapper.ToModel(reading);
        }

        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
    }

    public async IAsyncEnumerable<BlockDownloadChunk> DownloadBlockAsync(
        BlockDownloadRequest request,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        var block = blockStorage.GetBlock(request.BlockIdOrName);

        int count = 0;
        foreach (var slice in blockStorage.SliceBlockChunks(block, request.ChunkSize))
        {
            ct.ThrowIfCancellationRequested();
            count++;
            yield return CodeFirstContractMapper.ToModel(slice);
        }

        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
        await Task.CompletedTask;
    }

    public async Task<BlockUploadResult> UploadBlockAsync(
        IAsyncEnumerable<BlockUploadChunk> chunks,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        string blockName = string.Empty;
        string contentType = "application/octet-stream";
        using var ms = new MemoryStream();
        int chunkCount = 0;

        await foreach (var chunk in chunks.WithCancellation(ct))
        {
            if (!string.IsNullOrWhiteSpace(chunk.BlockName))
                blockName = chunk.BlockName;
            if (!string.IsNullOrWhiteSpace(chunk.ContentType))
                contentType = chunk.ContentType;
            if (chunk.ChunkData is { Length: > 0 })
            {
                ms.Write(chunk.ChunkData, 0, chunk.ChunkData.Length);
                chunkCount++;
            }
        }

        var saved = blockStorage.SaveBlock(blockName, contentType, ms.ToArray(), chunkCount);
        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, chunkCount.ToString());

        return new BlockUploadResult
        {
            BlockId = saved.BlockId,
            BlockName = saved.BlockName,
            TotalBytes = saved.TotalBytes,
            ChunkCount = saved.ChunkCount,
            Sha256Hex = saved.Sha256Hex
        };
    }

    public async Task<TelemetryBatchSummaryModel> IngestTelemetryBatchAsync(
        IAsyncEnumerable<TelemetryReadingModel> readings,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        var list = new List<TelemetryReading>();
        await foreach (var model in readings.WithCancellation(ct))
            list.Add(CodeFirstContractMapper.ToDomain(model));

        var summary = telemetryService.IngestBatch(list);
        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, summary.AcceptedCount.ToString());

        return new TelemetryBatchSummaryModel
        {
            AcceptedCount = summary.AcceptedCount,
            AnomalyCount = summary.AnomalyCount,
            AverageTemperatureCelsius = summary.AverageTemperatureCelsius,
            MaxCpuLoadPercent = summary.MaxCpuLoadPercent,
            AffectedWarehouses = [.. summary.AffectedWarehouses]
        };
    }

    public async IAsyncEnumerable<ChatStreamResponse> LiveOrderChatAsync(
        IAsyncEnumerable<ChatStreamRequest> requests,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        int processed = 0;

        await foreach (var msg in requests.WithCancellation(ct))
        {
            processed++;
            var reply = chatProcessor.Process(CodeFirstContractMapper.ToDomain(msg));
            yield return CodeFirstContractMapper.ToModel(reply);
        }

        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, processed.ToString());
    }

    public async IAsyncEnumerable<PriceQuoteResponse> ProcessPriceQuotesAsync(
        IAsyncEnumerable<PriceQuoteRequest> requests,
        CallContext context = default)
    {
        var ct = context.CancellationToken;
        decimal subtotal = 0m;
        int processed = 0;

        await foreach (var req in requests.WithCancellation(ct))
        {
            var quote = orderProcessing.CalculateQuote(req.Sku, req.Quantity, req.CustomerTier, subtotal);
            subtotal = quote.RunningSubtotal;
            processed++;
            yield return CodeFirstContractMapper.ToModel(quote);
        }

        context.ServerCallContext?.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, processed.ToString());
    }
}
