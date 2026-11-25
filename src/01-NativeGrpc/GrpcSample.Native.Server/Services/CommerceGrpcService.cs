using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services.Abstractions;
using GrpcSample.Native.Contracts;
using GrpcSample.Native.Server.Mapping;
using Microsoft.AspNetCore.Authorization;
using DomainProductCategory = GrpcSample.Domain.Models.Products.ProductCategory;

namespace GrpcSample.Native.Server.Services;

public sealed class CommerceGrpcService(
    IProductCatalogService productCatalog,
    IOrderProcessingService orderProcessing,
    ITelemetryService telemetryService,
    IBlockStorageService blockStorage,
    IOrderChatProcessor chatProcessor,
    IAuditService auditService) : CommerceService.CommerceServiceBase
{
    public override Task<ProductModel> GetProduct(GetProductRequest request, ServerCallContext context) =>
        Task.FromResult(NativeContractMapper.ToProto(productCatalog.GetProduct(request.Id)));

    public override Task<ListProductsResponse> ListProducts(ListProductsRequest request, ServerCallContext context)
    {
        var paged = productCatalog.ListProducts(NativeContractMapper.ToDomain(request));
        var response = new ListProductsResponse
        {
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
        response.Items.AddRange(paged.Items.Select(NativeContractMapper.ToProto));
        return Task.FromResult(response);
    }

    [Authorize]
    public override Task<ProductModel> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
        var created = productCatalog.CreateProduct(new CreateProductInput(
            Sku: request.Sku,
            Name: request.Name,
            Description: request.Description,
            Category: (DomainProductCategory)request.Category,
            Price: (decimal)request.Price,
            StockQuantity: request.StockQuantity,
            Rating: request.Rating,
            Tags: [.. request.Tags],
            Attributes: new Dictionary<string, string>(request.Attributes, StringComparer.OrdinalIgnoreCase)));

        return Task.FromResult(NativeContractMapper.ToProto(created));
    }

    [Authorize]
    public override Task<ProductModel> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        var updated = productCatalog.UpdateProduct(new UpdateProductInput(
            Id: request.Id,
            Name: request.Name,
            Description: request.Description,
            Category: (DomainProductCategory)request.Category,
            Price: (decimal)request.Price,
            StockQuantity: request.StockQuantity,
            IsActive: request.IsActive,
            Tags: [.. request.Tags],
            Attributes: new Dictionary<string, string>(request.Attributes, StringComparer.OrdinalIgnoreCase)));

        return Task.FromResult(NativeContractMapper.ToProto(updated));
    }

    [Authorize(Roles = "Admin")]
    public override Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context) =>
        Task.FromResult(new DeleteProductResponse
        {
            Deleted = productCatalog.DeleteProduct(request.Id),
            Id = request.Id
        });

    public override Task<OrderModel> GetOrder(GetOrderRequest request, ServerCallContext context) =>
        Task.FromResult(NativeContractMapper.ToProto(orderProcessing.GetOrder(request.Id)));

    public override Task<ListOrdersResponse> ListOrdersByCustomer(ListOrdersByCustomerRequest request, ServerCallContext context)
    {
        var orders = orderProcessing.ListOrdersByCustomer(request.CustomerId);
        var response = new ListOrdersResponse();
        response.Items.AddRange(orders.Select(NativeContractMapper.ToProto));
        return Task.FromResult(response);
    }

    [Authorize]
    public override Task<OrderModel> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var created = orderProcessing.CreateOrder(new CreateOrderInput(
            CustomerId: request.CustomerId,
            ShippingAddress: request.ShippingAddress,
            Items: request.Items.Select(i => new CreateOrderItemInput(i.ProductId, i.Quantity)).ToList()));

        return Task.FromResult(NativeContractMapper.ToProto(created));
    }

    public override Task<CatalogSummaryModel> GetCatalogSummary(Empty request, ServerCallContext context) =>
        Task.FromResult(NativeContractMapper.ToProto(productCatalog.GetCatalogSummary()));

    [Authorize(Roles = "Admin")]
    public override Task<AdminAuditInfoResponse> GetAdminAuditInfo(Empty request, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        var stats = auditService.GetStats();
        var response = new AdminAuditInfoResponse
        {
            TotalRequests = stats.TotalRequests,
            AuthenticatedUser = user.Identity?.Name ?? "unknown",
            Role = user.FindFirst(ClaimTypes.Role)?.Value ?? "unknown"
        };
        response.RecentEntries.AddRange(stats.RecentEntries.Select(e => new AuditEntryModel
        {
            Engine = e.Engine,
            Method = e.Method,
            CorrelationId = e.CorrelationId,
            User = e.User ?? "anonymous",
            ElapsedMs = e.ElapsedMs,
            Status = e.Status,
            TimestampUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(e.TimestampUtc, DateTimeKind.Utc))
        }));
        return Task.FromResult(response);
    }

    public override async Task<SlowOperationResponse> SimulateSlowOperation(SlowOperationRequest request, ServerCallContext context)
    {
        var delay = Math.Clamp(request.DelayMs, 0, 30_000);
        if (delay > 0)
            await Task.Delay(delay, context.CancellationToken);

        return new SlowOperationResponse
        {
            Label = string.IsNullOrWhiteSpace(request.Label) ? "slow-op" : request.Label,
            ActualDelayMs = delay,
            ServerEngine = "native-grpc"
        };
    }

    public override async Task StreamProducts(
        StreamProductsRequest request,
        IServerStreamWriter<ProductModel> responseStream,
        ServerCallContext context)
    {
        var items = productCatalog.StreamProducts(new ProductFilter(
            Category: request.HasCategory ? (DomainProductCategory)request.Category : null,
            ActiveOnly: request.ActiveOnly));

        int count = 0;
        foreach (var item in items)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (request.DelayMs > 0)
                await Task.Delay(Math.Min(request.DelayMs, 1000), context.CancellationToken);

            await responseStream.WriteAsync(NativeContractMapper.ToProto(item), context.CancellationToken);
            count++;
        }

        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
    }

    public override async Task SubscribeWarehouseTelemetry(
        TelemetrySubscriptionRequest request,
        IServerStreamWriter<TelemetryReadingModel> responseStream,
        ServerCallContext context)
    {
        var readings = telemetryService.GetWarehouseTelemetry(request.WarehouseId, request.AnomaliesOnly, request.MaxReadings);
        int count = 0;
        foreach (var reading in readings)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (request.IntervalMs > 0)
                await Task.Delay(Math.Min(request.IntervalMs, 1000), context.CancellationToken);

            await responseStream.WriteAsync(NativeContractMapper.ToProto(reading), context.CancellationToken);
            count++;
        }

        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
    }

    public override async Task DownloadBlock(
        BlockDownloadRequest request,
        IServerStreamWriter<BlockDownloadChunk> responseStream,
        ServerCallContext context)
    {
        var block = blockStorage.GetBlock(request.BlockIdOrName);
        int count = 0;

        foreach (var slice in blockStorage.SliceBlockChunks(block, request.ChunkSize))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await responseStream.WriteAsync(NativeContractMapper.ToProto(slice), context.CancellationToken);
            count++;
        }

        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, count.ToString());
    }

    public override async Task<BlockUploadResult> UploadBlock(
        IAsyncStreamReader<BlockUploadChunk> requestStream,
        ServerCallContext context)
    {
        string blockName = string.Empty;
        string contentType = "application/octet-stream";
        using var ms = new MemoryStream();
        int chunkCount = 0;

        await foreach (var chunk in requestStream.ReadAllAsync(context.CancellationToken))
        {
            switch (chunk.PayloadCase)
            {
                case BlockUploadChunk.PayloadOneofCase.Header:
                    blockName = chunk.Header.BlockName;
                    if (!string.IsNullOrWhiteSpace(chunk.Header.ContentType))
                        contentType = chunk.Header.ContentType;
                    break;
                case BlockUploadChunk.PayloadOneofCase.ChunkData:
                    chunk.ChunkData.WriteTo(ms);
                    chunkCount++;
                    break;
            }
        }

        var saved = blockStorage.SaveBlock(blockName, contentType, ms.ToArray(), chunkCount);
        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, chunkCount.ToString());

        return new BlockUploadResult
        {
            BlockId = saved.BlockId,
            BlockName = saved.BlockName,
            TotalBytes = saved.TotalBytes,
            ChunkCount = saved.ChunkCount,
            Sha256Hex = saved.Sha256Hex
        };
    }

    public override async Task<TelemetryBatchSummaryModel> IngestTelemetryBatch(
        IAsyncStreamReader<TelemetryReadingModel> requestStream,
        ServerCallContext context)
    {
        var items = new List<TelemetryReading>();
        await foreach (var model in requestStream.ReadAllAsync(context.CancellationToken))
            items.Add(NativeContractMapper.ToDomain(model));

        var summary = telemetryService.IngestBatch(items);
        var result = new TelemetryBatchSummaryModel
        {
            AcceptedCount = summary.AcceptedCount,
            AnomalyCount = summary.AnomalyCount,
            AverageTemperatureCelsius = summary.AverageTemperatureCelsius,
            MaxCpuLoadPercent = summary.MaxCpuLoadPercent
        };
        result.AffectedWarehouses.AddRange(summary.AffectedWarehouses);
        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, summary.AcceptedCount.ToString());
        return result;
    }

    public override async Task LiveOrderChat(
        IAsyncStreamReader<ChatStreamRequest> requestStream,
        IServerStreamWriter<ChatStreamResponse> responseStream,
        ServerCallContext context)
    {
        int processed = 0;
        await foreach (var msg in requestStream.ReadAllAsync(context.CancellationToken))
        {
            processed++;
            var reply = chatProcessor.Process(NativeContractMapper.ToDomain(msg));
            await responseStream.WriteAsync(NativeContractMapper.ToProto(reply), context.CancellationToken);
        }

        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, processed.ToString());
    }

    public override async Task ProcessPriceQuotes(
        IAsyncStreamReader<PriceQuoteRequest> requestStream,
        IServerStreamWriter<PriceQuoteResponse> responseStream,
        ServerCallContext context)
    {
        decimal subtotal = 0m;
        int processed = 0;

        await foreach (var req in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var quote = orderProcessing.CalculateQuote(
                req.Sku,
                req.Quantity,
                NativeContractMapper.ToDomain(req.CustomerTier),
                subtotal);
            subtotal = quote.RunningSubtotal;
            processed++;

            await responseStream.WriteAsync(NativeContractMapper.ToProto(quote), context.CancellationToken);
        }

        context.ResponseTrailers.Add(ApiKeyConstants.ItemsProcessedTrailer, processed.ToString());
    }
}
