using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;
using GrpcSample.CodeFirst.Contracts;
using GrpcSample.CodeFirst.Contracts.Models.Blocks;
using GrpcSample.CodeFirst.Contracts.Models.Common;
using GrpcSample.CodeFirst.Contracts.Models.Orders;
using GrpcSample.CodeFirst.Contracts.Models.Products;
using GrpcSample.CodeFirst.Contracts.Services;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Infrastructure.GrpcClient.Channels;
using GrpcSample.Infrastructure.GrpcClient.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.ClientFactory;
using ProtoBuf.Grpc.Reflection;

var grpcUrl = args.FirstOrDefault() ?? "http://localhost:5201";

Console.WriteLine($"=== 2. Code-First gRPC (protobuf-net.Grpc) Client Demo ({grpcUrl}) ===");

// 1. Runtime .proto generation via SchemaGenerator
var protoSchema = new SchemaGenerator().GetSchema<ICommerceCodeFirstService>();
Console.WriteLine($"[SchemaGenerator] Generated .proto schema length = {protoSchema.Length} chars");

// 2. Direct channel + CallInvoker
using var channel = GrpcChannelFactory.CreateResilientChannel(grpcUrl);
var invoker = channel.Intercept(new ClientCorrelationInterceptor("codefirst-1.0", ApiKeyConstants.AdminKey, "cf"));
var client = invoker.CreateGrpcService<ICommerceCodeFirstService>();

// 3. DI ClientFactory integration demo
var services = new ServiceCollection();
services.AddTransient(_ => new ClientCorrelationInterceptor("codefirst-1.0", ApiKeyConstants.AdminKey, "cf"));
services.AddCodeFirstGrpcClient<ICommerceCodeFirstService>(o => o.Address = new Uri(grpcUrl))
    .AddInterceptor<ClientCorrelationInterceptor>();
await using var sp = services.BuildServiceProvider();
var diClient = sp.GetRequiredService<ICommerceCodeFirstService>();

// Health Check
var health = await new Health.HealthClient(channel).CheckAsync(new HealthCheckRequest());
Console.WriteLine($"[Health] Status = {health.Status}");

// Reflection
using var reflCall = new ServerReflection.ServerReflectionClient(channel).ServerReflectionInfo();
await reflCall.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = "" });
await reflCall.RequestStream.CompleteAsync();
await foreach (var resp in reflCall.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"[Reflection] Services = {string.Join(", ", resp.ListServicesResponse.Service.Select(s => s.Name))}");
}

// Unary + CaptureMetadata
var callCtx = new CallContext(flags: CallContextFlags.CaptureMetadata);
var summary = await diClient.GetCatalogSummaryAsync(new Empty(), callCtx);
var responseHeaders = await callCtx.ResponseHeadersAsync();
var trailers = callCtx.ResponseTrailers();
Console.WriteLine($"[Summary] Products={summary.TotalProducts}, Orders={summary.TotalOrders}, Revenue={summary.TotalRevenue:C}");
Console.WriteLine($"[Metadata] x-server-engine={responseHeaders.GetValue(ApiKeyConstants.ServerEngineHeader)}, x-processing-time-ms={trailers.GetValue(ApiKeyConstants.ProcessingTimeTrailer)}");

// Unary Create
var created = await client.CreateProductAsync(new CreateProductRequest
{
    Sku = $"SKU-CF-{Random.Shared.Next(1000, 9999)}",
    Name = "Code-First Studio DAC",
    Description = "Created via protobuf-net.Grpc",
    Category = ProductCategory.Audio,
    Price = 249.50m,
    StockQuantity = 25,
    Tags = ["codefirst", "dac"],
    Attributes = new Dictionary<string, string> { ["chip"] = "ESS9039" }
});
Console.WriteLine($"[CreateProduct] {created.Id} -> {created.Name} (${created.Price})");

// Server Streaming via IAsyncEnumerable
int streamed = 0;
await foreach (var p in client.StreamProductsAsync(new StreamProductsRequest { Category = ProductCategory.Audio, ActiveOnly = true }))
{
    streamed++;
}
Console.WriteLine($"[StreamProducts:Audio] Streamed {streamed} items");

// Client Streaming (UploadBlock) + Server Streaming (DownloadBlock)
static async IAsyncEnumerable<BlockUploadChunk> ProduceChunks(byte[] data)
{
    yield return new BlockUploadChunk { BlockName = "codefirst-demo.bin", ContentType = "application/octet-stream" };
    for (int i = 0; i < data.Length; i += 512)
    {
        var len = Math.Min(512, data.Length - i);
        yield return new BlockUploadChunk { ChunkData = data.AsSpan(i, len).ToArray() };
        await Task.Yield();
    }
}

var payload = Encoding.UTF8.GetBytes(string.Join("|", Enumerable.Range(1, 150).Select(i => $"cf-block-{i}")));
var uploaded = await client.UploadBlockAsync(ProduceChunks(payload));
Console.WriteLine($"[UploadBlock] {uploaded.BlockId} bytes={uploaded.TotalBytes}");

using var dlStream = new MemoryStream();
await foreach (var chunk in client.DownloadBlockAsync(new BlockDownloadRequest { BlockIdOrName = uploaded.BlockId, ChunkSize = 512 }))
{
    dlStream.Write(chunk.ChunkData, 0, chunk.ChunkData.Length);
}
var sha = Convert.ToHexStringLower(SHA256.HashData(dlStream.ToArray()));
Console.WriteLine($"[DownloadBlock] SHA256 match = {sha == uploaded.Sha256Hex}");

// Bidirectional Streaming (PriceQuotes)
static async IAsyncEnumerable<PriceQuoteRequest> QuoteRequests()
{
    yield return new PriceQuoteRequest { Sku = "SKU-ELEC-001", Quantity = 3, CustomerTier = CustomerTier.Silver };
    await Task.Delay(20);
    yield return new PriceQuoteRequest { Sku = "SKU-HOME-001", Quantity = 10, CustomerTier = CustomerTier.Enterprise };
}

await foreach (var q in client.ProcessPriceQuotesAsync(QuoteRequests()))
{
    Console.WriteLine($"[Quote] {q.Sku} x{q.RequestedQuantity} -> Line={q.LineTotal:F2}, Subtotal={q.RunningSubtotal:F2}");
}

Console.WriteLine("Code-First gRPC client demo completed.");
