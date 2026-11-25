using System.Security.Claims;
using Grpc.Core;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Abstractions;
using GrpcSample.MagicOnion.Server.Filters;
using GrpcSample.MagicOnion.Server.Mapping;
using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Models.Audit;
using GrpcSample.MagicOnion.Shared.Models.Blocks;
using GrpcSample.MagicOnion.Shared.Models.Chat;
using GrpcSample.MagicOnion.Shared.Models.Common;
using GrpcSample.MagicOnion.Shared.Models.Orders;
using GrpcSample.MagicOnion.Shared.Models.Products;
using GrpcSample.MagicOnion.Shared.Models.Telemetry;
using GrpcSample.MagicOnion.Shared.Services;
using MagicOnion;
using MagicOnion.Server;

namespace GrpcSample.MagicOnion.Server.Services;

public sealed class CommerceMagicService(
    IProductCatalogService productCatalog,
    IOrderProcessingService orderProcessing,
    ITelemetryService telemetryService,
    IBlockStorageService blockStorage,
    IOrderChatProcessor chatProcessor,
    IAuditService auditService)
    : ServiceBase<ICommerceMagicService>, ICommerceMagicService
{
    public UnaryResult<ProductModel> GetProductAsync(string id) =>
        UnaryResult.FromResult(MagicOnionContractMapper.ToModel(productCatalog.GetProduct(id)));

    public UnaryResult<ListProductsResponse> ListProductsAsync(ListProductsRequest request)
    {
        var paged = productCatalog.ListProducts(new ProductFilter(
            Category: request.Category,
            SearchTerm: request.SearchTerm,
            ActiveOnly: request.ActiveOnly,
            MaxPrice: request.MaxPrice,
            Page: request.Page,
            PageSize: request.PageSize));

        return UnaryResult.FromResult(new ListProductsResponse
        {
            Items = paged.Items.Select(MagicOnionContractMapper.ToModel).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        });
    }

    [ApiKeyAuthorize]
    public UnaryResult<ProductModel> CreateProductAsync(CreateProductRequest request)
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

        return UnaryResult.FromResult(MagicOnionContractMapper.ToModel(created));
    }

    [ApiKeyAuthorize]
    public UnaryResult<ProductModel> UpdateProductAsync(UpdateProductRequest request)
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

        return UnaryResult.FromResult(MagicOnionContractMapper.ToModel(updated));
    }

    [ApiKeyAuthorize("Admin")]
    public UnaryResult<DeleteResponse> DeleteProductAsync(string id) =>
        UnaryResult.FromResult(new DeleteResponse
        {
            Deleted = productCatalog.DeleteProduct(id),
            Id = id
        });

    public UnaryResult<OrderModel> GetOrderAsync(string id) =>
        UnaryResult.FromResult(MagicOnionContractMapper.ToModel(orderProcessing.GetOrder(id)));

    public UnaryResult<List<OrderModel>> ListOrdersByCustomerAsync(string customerId) =>
        UnaryResult.FromResult(orderProcessing.ListOrdersByCustomer(customerId).Select(MagicOnionContractMapper.ToModel).ToList());

    [ApiKeyAuthorize]
    public UnaryResult<OrderModel> CreateOrderAsync(CreateOrderRequest request)
    {
        var created = orderProcessing.CreateOrder(new CreateOrderInput(
            CustomerId: request.CustomerId,
            ShippingAddress: request.ShippingAddress,
            Items: request.Items.Select(i => new CreateOrderItemInput(i.ProductId, i.Quantity)).ToList()));

        return UnaryResult.FromResult(MagicOnionContractMapper.ToModel(created));
    }

    public UnaryResult<CatalogSummaryModel> GetCatalogSummaryAsync() =>
        UnaryResult.FromResult(MagicOnionContractMapper.ToModel(productCatalog.GetCatalogSummary()));

    [ApiKeyAuthorize("Admin")]
    public UnaryResult<AdminAuditInfoResponse> GetAdminAuditInfoAsync()
    {
        var user = Context.CallContext.GetHttpContext().User;
        var stats = auditService.GetStats();
        return UnaryResult.FromResult(new AdminAuditInfoResponse
        {
            TotalRequests = stats.TotalRequests,
            AuthenticatedUser = user.Identity?.Name ?? "unknown",
            Role = user.FindFirst(ClaimTypes.Role)?.Value ?? "unknown",
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

    public async UnaryResult<SlowOperationResponse> SimulateSlowOperationAsync(int delayMs, string label)
    {
        var delay = Math.Clamp(delayMs, 0, 30_000);
        if (delay > 0)
            await Task.Delay(delay, Context.CallContext.CancellationToken);

        return new SlowOperationResponse
        {
            Label = string.IsNullOrWhiteSpace(label) ? "slow-op" : label,
            ActualDelayMs = delay,
            ServerEngine = "magiconion"
        };
    }

    public async Task<ServerStreamingResult<ProductModel>> StreamProductsAsync(StreamProductsRequest request)
    {
        var stream = GetServerStreamingContext<ProductModel>();
        var ct = Context.CallContext.CancellationToken;
        var items = productCatalog.StreamProducts(new ProductFilter(
            Category: request.Category,
            ActiveOnly: request.ActiveOnly));

        int count = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (request.DelayMs > 0)
                await Task.Delay(Math.Min(request.DelayMs, 1000), ct);

            await stream.WriteAsync(MagicOnionContractMapper.ToModel(item));
            count++;
        }

        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
        return stream.Result();
    }

    public async Task<ServerStreamingResult<TelemetryReadingModel>> SubscribeWarehouseTelemetryAsync(
        TelemetrySubscriptionRequest request)
    {
        var stream = GetServerStreamingContext<TelemetryReadingModel>();
        var ct = Context.CallContext.CancellationToken;
        var readings = telemetryService.GetWarehouseTelemetry(request.WarehouseId, request.AnomaliesOnly, request.MaxReadings);

        int count = 0;
        foreach (var reading in readings)
        {
            ct.ThrowIfCancellationRequested();
            if (request.IntervalMs > 0)
                await Task.Delay(Math.Min(request.IntervalMs, 1000), ct);

            await stream.WriteAsync(MagicOnionContractMapper.ToModel(reading));
            count++;
        }

        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
        return stream.Result();
    }

    public async Task<ServerStreamingResult<BlockDownloadChunk>> DownloadBlockAsync(string blockIdOrName, int chunkSize)
    {
        var stream = GetServerStreamingContext<BlockDownloadChunk>();
        var ct = Context.CallContext.CancellationToken;
        var block = blockStorage.GetBlock(blockIdOrName);

        int count = 0;
        foreach (var slice in blockStorage.SliceBlockChunks(block, chunkSize))
        {
            ct.ThrowIfCancellationRequested();
            await stream.WriteAsync(MagicOnionContractMapper.ToModel(slice));
            count++;
        }

        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
        return stream.Result();
    }

    public async Task<ClientStreamingResult<BlockUploadChunk, BlockUploadResult>> UploadBlockAsync()
    {
        var stream = GetClientStreamingContext<BlockUploadChunk, BlockUploadResult>();
        var ct = Context.CallContext.CancellationToken;
        string blockName = string.Empty;
        string contentType = "application/octet-stream";
        using var ms = new MemoryStream();
        int chunkCount = 0;

        while (await stream.MoveNext(ct))
        {
            var chunk = stream.Current;
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
        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, chunkCount.ToString());

        return stream.Result(new BlockUploadResult
        {
            BlockId = saved.BlockId,
            BlockName = saved.BlockName,
            TotalBytes = saved.TotalBytes,
            ChunkCount = saved.ChunkCount,
            Sha256Hex = saved.Sha256Hex
        });
    }

    public async Task<ClientStreamingResult<TelemetryReadingModel, TelemetryBatchSummaryModel>> IngestTelemetryBatchAsync()
    {
        var stream = GetClientStreamingContext<TelemetryReadingModel, TelemetryBatchSummaryModel>();
        var ct = Context.CallContext.CancellationToken;
        var list = new List<TelemetryReading>();

        while (await stream.MoveNext(ct))
            list.Add(MagicOnionContractMapper.ToDomain(stream.Current));

        var summary = telemetryService.IngestBatch(list);
        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, summary.AcceptedCount.ToString());

        return stream.Result(new TelemetryBatchSummaryModel
        {
            AcceptedCount = summary.AcceptedCount,
            AnomalyCount = summary.AnomalyCount,
            AverageTemperatureCelsius = summary.AverageTemperatureCelsius,
            MaxCpuLoadPercent = summary.MaxCpuLoadPercent,
            AffectedWarehouses = [.. summary.AffectedWarehouses]
        });
    }

    public async Task<DuplexStreamingResult<ChatStreamRequest, ChatStreamResponse>> LiveOrderChatAsync()
    {
        var stream = GetDuplexStreamingContext<ChatStreamRequest, ChatStreamResponse>();
        var ct = Context.CallContext.CancellationToken;
        int processed = 0;

        while (await stream.MoveNext(ct))
        {
            processed++;
            var reply = chatProcessor.Process(MagicOnionContractMapper.ToDomain(stream.Current));
            await stream.WriteAsync(MagicOnionContractMapper.ToModel(reply));
        }

        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, processed.ToString());
        return stream.Result();
    }

    public async Task<DuplexStreamingResult<PriceQuoteRequest, PriceQuoteResponse>> ProcessPriceQuotesAsync()
    {
        var stream = GetDuplexStreamingContext<PriceQuoteRequest, PriceQuoteResponse>();
        var ct = Context.CallContext.CancellationToken;
        decimal subtotal = 0m;
        int processed = 0;

        while (await stream.MoveNext(ct))
        {
            var req = stream.Current;
            var quote = orderProcessing.CalculateQuote(req.Sku, req.Quantity, req.CustomerTier, subtotal);
            subtotal = quote.RunningSubtotal;
            processed++;
            await stream.WriteAsync(MagicOnionContractMapper.ToModel(quote));
        }

        Context.CallContext.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, processed.ToString());
        return stream.Result();
    }
}
