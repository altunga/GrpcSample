using System.Net;
using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Reflection.V1Alpha;
using GrpcSample.CodeFirst.Contracts;
using GrpcSample.CodeFirst.Contracts.Models.Blocks;
using GrpcSample.CodeFirst.Contracts.Models.Chat;
using GrpcSample.CodeFirst.Contracts.Models.Common;
using GrpcSample.CodeFirst.Contracts.Models.Orders;
using GrpcSample.CodeFirst.Contracts.Models.Products;
using GrpcSample.CodeFirst.Contracts.Models.Telemetry;
using GrpcSample.CodeFirst.Contracts.Services;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Reflection;
using Xunit;

namespace GrpcSample.CodeFirst.Tests;

public sealed class CodeFirstGrpcIntegrationTests(CodeFirstGrpcServerFixture fixture)
    : IClassFixture<CodeFirstGrpcServerFixture>
{
    [Fact]
    public async Task HealthCheck_Reflection_And_SchemaProtoEndpoint_Work()
    {
        var health = await new Health.HealthClient(fixture.Channel).CheckAsync(new HealthCheckRequest());
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, health.Status);

        var reflectionClient = new ServerReflection.ServerReflectionClient(fixture.Channel);
        using var reflCall = reflectionClient.ServerReflectionInfo();
        await reflCall.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = "" });
        await reflCall.RequestStream.CompleteAsync();

        var services = new List<string>();
        await foreach (var r in reflCall.ResponseStream.ReadAllAsync())
            services.AddRange(r.ListServicesResponse.Service.Select(s => s.Name));
        Assert.Contains("commerce.codefirst.v1.CommerceCodeFirstService", services);

        var localSchema = new SchemaGenerator().GetSchema<ICommerceCodeFirstService>();
        Assert.Contains("service CommerceCodeFirstService", localSchema);

        using var http = new HttpClient { BaseAddress = fixture.HttpUri };
        var httpResp = await http.GetAsync("/schema.proto");
        Assert.Equal(HttpStatusCode.OK, httpResp.StatusCode);
        var remoteSchema = await httpResp.Content.ReadAsStringAsync();
        Assert.Contains("CommerceCodeFirstService", remoteSchema);
    }

    [Fact]
    public async Task Unary_ProductCrud_List_And_Summary_WorkWithMetadataAndTrailers()
    {
        var client = fixture.CreateClient(ApiKeyConstants.AdminKey);
        var corrId = "cf-test-corr-001";
        var callCtx = new CallContext(
            new CallOptions(headers: new Metadata { { ApiKeyConstants.CorrelationIdHeader, corrId } }),
            flags: CallContextFlags.CaptureMetadata);

        var p1 = await client.GetProductAsync(new IdRequest { Id = "PRD-001" }, callCtx);
        var respHeaders = await callCtx.ResponseHeadersAsync();
        var trailers = callCtx.ResponseTrailers();

        Assert.Equal("PRD-001", p1.Id);
        Assert.Equal("protobuf-net-grpc", respHeaders.GetValue(ApiKeyConstants.ServerEngineHeader));
        Assert.Equal(corrId, respHeaders.GetValue(ApiKeyConstants.CorrelationIdHeader));
        Assert.Equal("OK", trailers.GetValue(ApiKeyConstants.RequestStatusTrailer));
        Assert.NotNull(trailers.GetValue(ApiKeyConstants.ProcessingTimeTrailer));

        var list = await client.ListProductsAsync(new ListProductsRequest
        {
            Category = ProductCategory.Books,
            ActiveOnly = true,
            Page = 1,
            PageSize = 5
        });
        Assert.Equal(10, list.TotalCount);
        Assert.Equal(5, list.Items.Count);

        var created = await client.CreateProductAsync(new CreateProductRequest
        {
            Sku = "SKU-CF-TEST-1",
            Name = "CodeFirst Test Item",
            Description = "Created in test",
            Category = ProductCategory.Gaming,
            Price = 149.90m,
            StockQuantity = 35,
            Rating = 4.9,
            Tags = ["cf"],
            Attributes = new Dictionary<string, string> { ["env"] = "xunit" }
        });
        Assert.StartsWith("PRD-", created.Id);

        var updated = await client.UpdateProductAsync(new UpdateProductRequest
        {
            Id = created.Id,
            Name = "CodeFirst Test Item V2",
            Description = "Updated",
            Category = ProductCategory.Audio,
            Price = 169.90m,
            StockQuantity = 20,
            IsActive = true
        });
        Assert.Equal("CodeFirst Test Item V2", updated.Name);
        Assert.Equal(ProductCategory.Audio, updated.Category);

        var deleted = await client.DeleteProductAsync(new IdRequest { Id = created.Id });
        Assert.True(deleted.Deleted);

        var summary = await client.GetCatalogSummaryAsync(new Empty());
        Assert.True(summary.TotalProducts >= 60);
    }

    [Fact]
    public async Task Unary_Orders_And_AdminAuditInfo_WorkEndToEnd()
    {
        var client = fixture.CreateClient(ApiKeyConstants.AdminKey);

        var seededOrder = await client.GetOrderAsync(new IdRequest { Id = "ORD-1001" });
        Assert.Equal("ORD-1001", seededOrder.Id);

        var customerOrders = await client.ListOrdersByCustomerAsync(new IdRequest { Id = "CUS-001" });
        Assert.NotEmpty(customerOrders.Items);

        var createdOrder = await client.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "CUS-003",
            ShippingAddress = "77 CodeFirst Ave, Izmir",
            Items = [new CreateOrderLineRequest { ProductId = "PRD-003", Quantity = 2 }]
        });
        Assert.StartsWith("ORD-", createdOrder.Id);
        Assert.Equal(OrderStatus.Pending, createdOrder.Status);

        var audit = await client.GetAdminAuditInfoAsync(new Empty());
        Assert.Equal("admin@grpcsample.dev", audit.AuthenticatedUser);
        Assert.Equal("Admin", audit.Role);
        Assert.True(audit.TotalRequests > 0);
    }

    [Fact]
    public async Task ErrorCodes_Auth_And_Deadlines_MapProperly()
    {
        var anonClient = fixture.CreateClient();
        var userClient = fixture.CreateClient(ApiKeyConstants.UserKey);
        var adminClient = fixture.CreateClient(ApiKeyConstants.AdminKey);

        var exUnauth = await Assert.ThrowsAsync<RpcException>(async () =>
            await anonClient.CreateProductAsync(new CreateProductRequest { Sku = "X", Name = "X", Price = 10 }));
        Assert.Equal(StatusCode.Unauthenticated, exUnauth.StatusCode);

        var exForbidden = await Assert.ThrowsAsync<RpcException>(async () =>
            await userClient.DeleteProductAsync(new IdRequest { Id = "PRD-001" }));
        Assert.Equal(StatusCode.PermissionDenied, exForbidden.StatusCode);

        var exAuditForbidden = await Assert.ThrowsAsync<RpcException>(async () =>
            await userClient.GetAdminAuditInfoAsync(new Empty()));
        Assert.Equal(StatusCode.PermissionDenied, exAuditForbidden.StatusCode);

        var exNotFound = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.GetProductAsync(new IdRequest { Id = "PRD-9999" }));
        Assert.Equal(StatusCode.NotFound, exNotFound.StatusCode);

        var exDup = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.CreateProductAsync(new CreateProductRequest
            {
                Sku = "SKU-ELEC-001",
                Name = "Duplicate",
                Price = 10,
                StockQuantity = 5
            }));
        Assert.Equal(StatusCode.AlreadyExists, exDup.StatusCode);

        var exInvalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.CreateProductAsync(new CreateProductRequest { Sku = "", Name = "", Price = -5 }));
        Assert.Equal(StatusCode.InvalidArgument, exInvalid.StatusCode);

        var exPrecond = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.CreateOrderAsync(new CreateOrderRequest
            {
                CustomerId = "CUS-001",
                ShippingAddress = "Test",
                Items = [new CreateOrderLineRequest { ProductId = "PRD-001", Quantity = 50000 }]
            }));
        Assert.Equal(StatusCode.FailedPrecondition, exPrecond.StatusCode);

        var exDeadline = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.SimulateSlowOperationAsync(
                new SlowOperationRequest { DelayMs = 2000, Label = "timeout" },
                new CallContext(new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(80)))));
        Assert.Equal(StatusCode.DeadlineExceeded, exDeadline.StatusCode);

        using var cts = new CancellationTokenSource(80);
        var exCancel = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.SimulateSlowOperationAsync(
                new SlowOperationRequest { DelayMs = 2000, Label = "cancel" },
                new CallContext(new CallOptions(cancellationToken: cts.Token))));
        Assert.Equal(StatusCode.Cancelled, exCancel.StatusCode);

        var okSlow = await adminClient.SimulateSlowOperationAsync(new SlowOperationRequest { DelayMs = 15, Label = "ok" });
        Assert.Equal("protobuf-net-grpc", okSlow.ServerEngine);
    }

    [Fact]
    public async Task ServerStreaming_Products_Telemetry_And_MidStreamCancellation()
    {
        var client = fixture.CreateClient();

        var products = new List<ProductModel>();
        await foreach (var p in client.StreamProductsAsync(new StreamProductsRequest
        {
            Category = ProductCategory.Audio,
            ActiveOnly = true
        }))
        {
            products.Add(p);
        }
        Assert.Equal(10, products.Count);

        using var cts = new CancellationTokenSource();
        var partial = new List<ProductModel>();
        await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var p in client.StreamProductsAsync(
                new StreamProductsRequest { DelayMs = 30 },
                new CallContext(new CallOptions(cancellationToken: cts.Token))))
            {
                partial.Add(p);
                if (partial.Count == 2) cts.Cancel();
            }
        });
        Assert.Equal(2, partial.Count);

        var readings = new List<TelemetryReadingModel>();
        await foreach (var r in client.SubscribeWarehouseTelemetryAsync(new TelemetrySubscriptionRequest
        {
            WarehouseId = "WH-IST-01",
            AnomaliesOnly = true,
            MaxReadings = 10
        }))
        {
            readings.Add(r);
        }
        Assert.NotEmpty(readings);
        Assert.All(readings, r => Assert.True(r.IsAnomaly && r.WarehouseId == "WH-IST-01"));
    }

    [Fact]
    public async Task ClientStreaming_UploadBlock_IngestTelemetry_And_DownloadBlock_WithCompression()
    {
        var client = fixture.CreateClient(ApiKeyConstants.UserKey);
        var payload = Encoding.UTF8.GetBytes(string.Join("-", Enumerable.Range(1, 1200).Select(i => $"cf-block-{i}")));

        static async IAsyncEnumerable<BlockUploadChunk> Produce(byte[] bytes)
        {
            yield return new BlockUploadChunk { BlockName = "integration-cf.bin", ContentType = "application/octet-stream" };
            for (int i = 0; i < bytes.Length; i += 1024)
            {
                var len = Math.Min(1024, bytes.Length - i);
                yield return new BlockUploadChunk { ChunkData = bytes.AsSpan(i, len).ToArray() };
                await Task.Yield();
            }
        }

        var gzipCtx = new CallContext(new CallOptions(headers: new Metadata { { "grpc-internal-encoding-request", "gzip" } }));
        var uploadResult = await client.UploadBlockAsync(Produce(payload), gzipCtx);
        Assert.Equal(payload.Length, uploadResult.TotalBytes);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(payload)), uploadResult.Sha256Hex);

        using var ms = new MemoryStream();
        await foreach (var chunk in client.DownloadBlockAsync(new BlockDownloadRequest { BlockIdOrName = uploadResult.BlockId, ChunkSize = 1024 }))
            ms.Write(chunk.ChunkData, 0, chunk.ChunkData.Length);

        Assert.Equal(payload, ms.ToArray());

        static async IAsyncEnumerable<TelemetryReadingModel> ProduceTelemetry()
        {
            yield return new TelemetryReadingModel { WarehouseId = "WH-BER-04", SensorId = "S1", TemperatureCelsius = 20.5, HumidityPercent = 41, CpuLoadPercent = 32 };
            yield return new TelemetryReadingModel { WarehouseId = "WH-LON-05", SensorId = "S2", TemperatureCelsius = 37.5, HumidityPercent = 52, CpuLoadPercent = 93 };
            await Task.CompletedTask;
        }

        var ingestSummary = await client.IngestTelemetryBatchAsync(ProduceTelemetry());
        Assert.Equal(2, ingestSummary.AcceptedCount);
        Assert.Equal(1, ingestSummary.AnomalyCount);
        Assert.Equal(93.0, ingestSummary.MaxCpuLoadPercent);
    }

    [Fact]
    public async Task BidirectionalStreaming_LiveOrderChat_And_ProcessPriceQuotes()
    {
        var client = fixture.CreateClient();

        static async IAsyncEnumerable<ChatStreamRequest> ChatInputs()
        {
            yield return new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.Join };
            yield return new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.Ping };
            yield return new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.OrderStatusQuery, OrderId = "ORD-1001" };
            yield return new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.Message, Text = "Checking order" };
            await Task.CompletedTask;
        }

        var chatResponses = new List<ChatStreamResponse>();
        await foreach (var r in client.LiveOrderChatAsync(ChatInputs()))
            chatResponses.Add(r);

        Assert.Equal(4, chatResponses.Count);
        Assert.Contains(chatResponses, r => r.Text == "PONG");
        Assert.Contains(chatResponses, r => r.Text.Contains("ORD-1001"));
        Assert.Contains(chatResponses, r => r.Text.Contains("Echo[order-support]: Checking order"));

        static async IAsyncEnumerable<PriceQuoteRequest> QuoteInputs()
        {
            yield return new PriceQuoteRequest { Sku = "SKU-ELEC-001", Quantity = 2, CustomerTier = CustomerTier.Gold };
            yield return new PriceQuoteRequest { Sku = "SKU-BOOK-001", Quantity = 10, CustomerTier = CustomerTier.Enterprise };
            await Task.CompletedTask;
        }

        var quotes = new List<PriceQuoteResponse>();
        await foreach (var q in client.ProcessPriceQuotesAsync(QuoteInputs()))
            quotes.Add(q);

        Assert.Equal(2, quotes.Count);
        Assert.Equal(10m, quotes[0].DiscountPercent);
        Assert.Equal(20m, quotes[1].DiscountPercent);
        Assert.True(quotes[1].RunningSubtotal > quotes[0].RunningSubtotal);
    }
}
