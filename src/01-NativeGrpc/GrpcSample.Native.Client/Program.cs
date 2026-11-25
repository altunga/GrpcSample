using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Health.V1;
using Grpc.Reflection.V1Alpha;
using GrpcSample.Domain.Auth;
using GrpcSample.Infrastructure.GrpcClient.Channels;
using GrpcSample.Infrastructure.GrpcClient.Interceptors;
using GrpcSample.Native.Contracts;

var grpcUrl = args.FirstOrDefault() ?? "http://localhost:5101";
var restUrl = args.Skip(1).FirstOrDefault() ?? "http://localhost:5100";

using var channel = GrpcChannelFactory.CreateResilientChannel(grpcUrl);
var invoker = channel.Intercept(new ClientCorrelationInterceptor("native-1.0", ApiKeyConstants.AdminKey, "nat"));
var client = new CommerceService.CommerceServiceClient(invoker);

Console.WriteLine($"=== 1. Native Proto-First gRPC Client Demo ({grpcUrl}) ===");

// 1. Health Check
var healthClient = new Health.HealthClient(channel);
var health = await healthClient.CheckAsync(new HealthCheckRequest());
Console.WriteLine($"[Health] Status = {health.Status}");

// 2. Server Reflection
var reflectionClient = new ServerReflection.ServerReflectionClient(channel);
using var reflectionCall = reflectionClient.ServerReflectionInfo();
await reflectionCall.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = "" });
await reflectionCall.RequestStream.CompleteAsync();
await foreach (var resp in reflectionCall.ResponseStream.ReadAllAsync())
{
    var services = string.Join(", ", resp.ListServicesResponse.Service.Select(s => s.Name));
    Console.WriteLine($"[Reflection] Services = {services}");
}

// 3. Unary + Response Headers & Trailers
var summaryCall = client.GetCatalogSummaryAsync(new Empty());
var summary = await summaryCall.ResponseAsync;
var headers = await summaryCall.ResponseHeadersAsync;
var trailers = summaryCall.GetTrailers();
Console.WriteLine($"[Summary] Products={summary.TotalProducts}, Orders={summary.TotalOrders}, Revenue={summary.TotalRevenue:C}");
Console.WriteLine($"[Metadata] x-server-engine={headers.GetValue(ApiKeyConstants.ServerEngineHeader)}, x-processing-time-ms={trailers.GetValue(ApiKeyConstants.ProcessingTimeTrailer)}");

// 4. Unary CRUD
var created = await client.CreateProductAsync(new CreateProductRequest
{
    Sku = $"SKU-DEMO-{Random.Shared.Next(1000, 9999)}",
    Name = "Native Demo Mechanical Switch Kit",
    Description = "Created via Native gRPC client",
    Category = ProductCategory.Gaming,
    Price = 69.90,
    StockQuantity = 40,
    Rating = 4.9,
    Tags = { "demo", "native" },
    Attributes = { ["switch"] = "linear" }
});
Console.WriteLine($"[CreateProduct] {created.Id} / {created.Sku} -> {created.Name}");

// 5. Server Streaming
Console.Write("[StreamProducts:Gaming] ");
using var productStream = client.StreamProducts(new StreamProductsRequest { Category = ProductCategory.Gaming, ActiveOnly = true });
int streamedCount = 0;
await foreach (var p in productStream.ResponseStream.ReadAllAsync())
{
    streamedCount++;
    if (streamedCount <= 3) Console.Write($"{p.Id} ");
}
Console.WriteLine($"(total streamed: {streamedCount})");

// 6. Client Streaming (UploadBlock) + Server Streaming (DownloadBlock)
using var uploadCall = client.UploadBlock();
await uploadCall.RequestStream.WriteAsync(new BlockUploadChunk
{
    Header = new BlockHeader { BlockName = "native-demo.bin", ContentType = "application/octet-stream" }
});
var payload = Encoding.UTF8.GetBytes(string.Join("|", Enumerable.Range(1, 200).Select(i => $"chunk-payload-{i}")));
for (int i = 0; i < payload.Length; i += 512)
{
    var slice = payload.AsMemory(i, Math.Min(512, payload.Length - i));
    await uploadCall.RequestStream.WriteAsync(new BlockUploadChunk { ChunkData = UnsafeByteOperations.UnsafeWrap(slice) });
}
await uploadCall.RequestStream.CompleteAsync();
var uploadResult = await uploadCall.ResponseAsync;
Console.WriteLine($"[UploadBlock] {uploadResult.BlockId} bytes={uploadResult.TotalBytes} sha256={uploadResult.Sha256Hex[..12]}...");

using var downloadCall = client.DownloadBlock(new BlockDownloadRequest { BlockIdOrName = uploadResult.BlockId, ChunkSize = 512 });
using var downloaded = new MemoryStream();
await foreach (var chunk in downloadCall.ResponseStream.ReadAllAsync())
{
    chunk.ChunkData.WriteTo(downloaded);
}
var dlSha = Convert.ToHexStringLower(SHA256.HashData(downloaded.ToArray()));
Console.WriteLine($"[DownloadBlock] Verified SHA256 match = {dlSha == uploadResult.Sha256Hex}");

// 7. Bidirectional Streaming (Price Quotes)
using var quoteCall = client.ProcessPriceQuotes();
var quoteReadTask = Task.Run(async () =>
{
    await foreach (var q in quoteCall.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"[Quote] {q.Sku} x{q.RequestedQuantity} -> Line={q.LineTotal:F2}, Subtotal={q.RunningSubtotal:F2} ({q.Note})");
    }
});
await quoteCall.RequestStream.WriteAsync(new PriceQuoteRequest { Sku = "SKU-ELEC-001", Quantity = 2, CustomerTier = CustomerTier.Gold });
await quoteCall.RequestStream.WriteAsync(new PriceQuoteRequest { Sku = "SKU-BOOK-001", Quantity = 5, CustomerTier = CustomerTier.Enterprise });
await quoteCall.RequestStream.CompleteAsync();
await quoteReadTask;

// 8. JSON Transcoding (HTTP/1.1 REST)
try
{
    using var http = new HttpClient { BaseAddress = new Uri(restUrl) };
    var restProduct = await http.GetFromJsonAsync<Dictionary<string, object>>("/v1/products/PRD-001");
    Console.WriteLine($"[JSON Transcoding] GET /v1/products/PRD-001 -> name={restProduct?["name"]}");
}
catch (Exception ex)
{
    Console.WriteLine($"[JSON Transcoding] Skipped/Error: {ex.Message}");
}

Console.WriteLine("Native gRPC client demo completed.");
