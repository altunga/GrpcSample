using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Health.V1;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Infrastructure.GrpcClient.Channels;
using GrpcSample.MagicOnion.Client.Filters;
using GrpcSample.MagicOnion.Client.Receivers;
using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Models.Blocks;
using GrpcSample.MagicOnion.Shared.Models.Orders;
using GrpcSample.MagicOnion.Shared.Models.Products;
using GrpcSample.MagicOnion.Shared.Hubs;
using GrpcSample.MagicOnion.Shared.Services;
using MagicOnion.Client;

var grpcUrl = args.FirstOrDefault() ?? "http://localhost:5301";
Console.WriteLine($"=== 3. MagicOnion v7 Client Demo ({grpcUrl}) ===");

using var channel = GrpcChannelFactory.CreateResilientChannel(grpcUrl);
var client = MagicOnionClient.Create<ICommerceMagicService>(
        channel,
        [new ClientCorrelationFilter(ApiKeyConstants.AdminKey)])
    .WithHeaders(new Metadata());

// 1. Health Check
var health = await new Health.HealthClient(channel).CheckAsync(new HealthCheckRequest());
Console.WriteLine($"[Health] Status = {health.Status}");

// 2. Unary + Response Headers & Trailers
var summaryCall = client.GetCatalogSummaryAsync();
var summary = await summaryCall.ResponseAsync;
var headers = await summaryCall.ResponseHeadersAsync;
var trailers = summaryCall.GetTrailers();
Console.WriteLine($"[Summary] Products={summary.TotalProducts}, Orders={summary.TotalOrders}, Revenue={summary.TotalRevenue:C}");
Console.WriteLine($"[Metadata] x-server-engine={headers.GetValue(ApiKeyConstants.ServerEngineHeader)}, x-processing-time-ms={trailers.GetValue(ApiKeyConstants.ProcessingTimeTrailer)}");

// 3. Unary Create
var created = await client.CreateProductAsync(new CreateProductRequest
{
    Sku = $"SKU-MO-{Random.Shared.Next(1000, 9999)}",
    Name = "MagicOnion Low-Latency Controller",
    Description = "Created via MagicOnion v7 MessagePack RPC",
    Category = ProductCategory.Gaming,
    Price = 129.90m,
    StockQuantity = 60,
    Tags = ["magiconion", "messagepack"],
    Attributes = new Dictionary<string, string> { ["polling_hz"] = "8000" }
});
Console.WriteLine($"[CreateProduct] {created.Id} -> {created.Name}");

// 4. Server Streaming
using var productStream = await client.StreamProductsAsync(new StreamProductsRequest { Category = ProductCategory.Gaming, ActiveOnly = true });
int streamedCount = 0;
await foreach (var _ in productStream.ResponseStream.ReadAllAsync())
    streamedCount++;
Console.WriteLine($"[StreamProducts:Gaming] Streamed {streamedCount} items");

// 5. Client Streaming (UploadBlock) + Server Streaming (DownloadBlock)
using var uploadStream = await client.UploadBlockAsync();
await uploadStream.RequestStream.WriteAsync(new BlockUploadChunk
{
    BlockName = "magiconion-demo.bin",
    ContentType = "application/octet-stream"
});
var payload = Encoding.UTF8.GetBytes(string.Join("|", Enumerable.Range(1, 150).Select(i => $"mo-block-{i}")));
for (int i = 0; i < payload.Length; i += 512)
{
    var len = Math.Min(512, payload.Length - i);
    await uploadStream.RequestStream.WriteAsync(new BlockUploadChunk { ChunkData = payload.AsSpan(i, len).ToArray() });
}
await uploadStream.RequestStream.CompleteAsync();
var uploadResult = await uploadStream.ResponseAsync;
Console.WriteLine($"[UploadBlock] {uploadResult.BlockId} bytes={uploadResult.TotalBytes}");

using var dlStream = await client.DownloadBlockAsync(uploadResult.BlockId, 512);
using var dlMem = new MemoryStream();
await foreach (var chunk in dlStream.ResponseStream.ReadAllAsync())
    dlMem.Write(chunk.ChunkData, 0, chunk.ChunkData.Length);
var dlSha = Convert.ToHexStringLower(SHA256.HashData(dlMem.ToArray()));
Console.WriteLine($"[DownloadBlock] SHA256 match = {dlSha == uploadResult.Sha256Hex}");

// 6. Bidirectional Streaming (Price Quotes)
using var quoteStream = await client.ProcessPriceQuotesAsync();
var quoteReadTask = Task.Run(async () =>
{
    await foreach (var q in quoteStream.ResponseStream.ReadAllAsync())
        Console.WriteLine($"[Quote] {q.Sku} x{q.RequestedQuantity} -> Line={q.LineTotal:F2}, Subtotal={q.RunningSubtotal:F2}");
});
await quoteStream.RequestStream.WriteAsync(new PriceQuoteRequest { Sku = "SKU-ELEC-001", Quantity = 2, CustomerTier = CustomerTier.Gold });
await quoteStream.RequestStream.WriteAsync(new PriceQuoteRequest { Sku = "SKU-AUDI-001", Quantity = 5, CustomerTier = CustomerTier.Enterprise });
await quoteStream.RequestStream.CompleteAsync();
await quoteReadTask;

// 7. StreamingHub Real-Time Pub/Sub
var receiver = new ConsoleHubReceiver();
var hub = await StreamingHubClient.ConnectAsync<ICommerceHub, ICommerceHubReceiver>(channel, receiver);
var snapshot = await hub.JoinRoomAsync("order-support", "demo-console");
Console.WriteLine($"[StreamingHub] Joined '{snapshot.RoomName}' (history={snapshot.RecentHistory.Count})");
await hub.SendMessageAsync("Hello from MagicOnion v7 StreamingHub!");
await hub.BroadcastOrderUpdateAsync("ORD-1001", "Shipped");
await Task.Delay(150);
await hub.LeaveRoomAsync();
await hub.DisposeAsync();

Console.WriteLine("MagicOnion v7 client demo completed.");
