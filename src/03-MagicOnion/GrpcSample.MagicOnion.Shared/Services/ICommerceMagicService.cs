using GrpcSample.MagicOnion.Shared.Models.Audit;
using GrpcSample.MagicOnion.Shared.Models.Blocks;
using GrpcSample.MagicOnion.Shared.Models.Chat;
using GrpcSample.MagicOnion.Shared.Models.Common;
using GrpcSample.MagicOnion.Shared.Models.Orders;
using GrpcSample.MagicOnion.Shared.Models.Products;
using GrpcSample.MagicOnion.Shared.Models.Telemetry;
using MagicOnion;

namespace GrpcSample.MagicOnion.Shared.Services;

public interface ICommerceMagicService : IService<ICommerceMagicService>
{
    UnaryResult<ProductModel> GetProductAsync(string id);
    UnaryResult<ListProductsResponse> ListProductsAsync(ListProductsRequest request);
    UnaryResult<ProductModel> CreateProductAsync(CreateProductRequest request);
    UnaryResult<ProductModel> UpdateProductAsync(UpdateProductRequest request);
    UnaryResult<DeleteResponse> DeleteProductAsync(string id);

    UnaryResult<OrderModel> GetOrderAsync(string id);
    UnaryResult<List<OrderModel>> ListOrdersByCustomerAsync(string customerId);
    UnaryResult<OrderModel> CreateOrderAsync(CreateOrderRequest request);

    UnaryResult<CatalogSummaryModel> GetCatalogSummaryAsync();
    UnaryResult<AdminAuditInfoResponse> GetAdminAuditInfoAsync();
    UnaryResult<SlowOperationResponse> SimulateSlowOperationAsync(int delayMs, string label);

    Task<ServerStreamingResult<ProductModel>> StreamProductsAsync(StreamProductsRequest request);
    Task<ServerStreamingResult<TelemetryReadingModel>> SubscribeWarehouseTelemetryAsync(TelemetrySubscriptionRequest request);
    Task<ServerStreamingResult<BlockDownloadChunk>> DownloadBlockAsync(string blockIdOrName, int chunkSize);

    Task<ClientStreamingResult<BlockUploadChunk, BlockUploadResult>> UploadBlockAsync();
    Task<ClientStreamingResult<TelemetryReadingModel, TelemetryBatchSummaryModel>> IngestTelemetryBatchAsync();

    Task<DuplexStreamingResult<ChatStreamRequest, ChatStreamResponse>> LiveOrderChatAsync();
    Task<DuplexStreamingResult<PriceQuoteRequest, PriceQuoteResponse>> ProcessPriceQuotesAsync();
}
