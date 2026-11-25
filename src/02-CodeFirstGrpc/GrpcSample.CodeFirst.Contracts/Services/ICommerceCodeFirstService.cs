using GrpcSample.CodeFirst.Contracts.Models.Audit;
using GrpcSample.CodeFirst.Contracts.Models.Blocks;
using GrpcSample.CodeFirst.Contracts.Models.Chat;
using GrpcSample.CodeFirst.Contracts.Models.Common;
using GrpcSample.CodeFirst.Contracts.Models.Orders;
using GrpcSample.CodeFirst.Contracts.Models.Products;
using GrpcSample.CodeFirst.Contracts.Models.Telemetry;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GrpcSample.CodeFirst.Contracts.Services;

[Service("commerce.codefirst.v1.CommerceCodeFirstService")]
public interface ICommerceCodeFirstService
{
    ValueTask<ProductModel> GetProductAsync(IdRequest request, CallContext context = default);
    ValueTask<ListProductsResponse> ListProductsAsync(ListProductsRequest request, CallContext context = default);
    ValueTask<ProductModel> CreateProductAsync(CreateProductRequest request, CallContext context = default);
    ValueTask<ProductModel> UpdateProductAsync(UpdateProductRequest request, CallContext context = default);
    ValueTask<DeleteResponse> DeleteProductAsync(IdRequest request, CallContext context = default);

    ValueTask<OrderModel> GetOrderAsync(IdRequest request, CallContext context = default);
    ValueTask<ListOrdersResponse> ListOrdersByCustomerAsync(IdRequest request, CallContext context = default);
    ValueTask<OrderModel> CreateOrderAsync(CreateOrderRequest request, CallContext context = default);

    ValueTask<CatalogSummaryModel> GetCatalogSummaryAsync(Empty request, CallContext context = default);
    ValueTask<AdminAuditInfoResponse> GetAdminAuditInfoAsync(Empty request, CallContext context = default);
    ValueTask<SlowOperationResponse> SimulateSlowOperationAsync(SlowOperationRequest request, CallContext context = default);

    IAsyncEnumerable<ProductModel> StreamProductsAsync(StreamProductsRequest request, CallContext context = default);
    IAsyncEnumerable<TelemetryReadingModel> SubscribeWarehouseTelemetryAsync(TelemetrySubscriptionRequest request, CallContext context = default);
    IAsyncEnumerable<BlockDownloadChunk> DownloadBlockAsync(BlockDownloadRequest request, CallContext context = default);

    Task<BlockUploadResult> UploadBlockAsync(IAsyncEnumerable<BlockUploadChunk> chunks, CallContext context = default);
    Task<TelemetryBatchSummaryModel> IngestTelemetryBatchAsync(IAsyncEnumerable<TelemetryReadingModel> readings, CallContext context = default);

    IAsyncEnumerable<ChatStreamResponse> LiveOrderChatAsync(IAsyncEnumerable<ChatStreamRequest> requests, CallContext context = default);
    IAsyncEnumerable<PriceQuoteResponse> ProcessPriceQuotesAsync(IAsyncEnumerable<PriceQuoteRequest> requests, CallContext context = default);
}
