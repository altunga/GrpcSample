using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Reflection.V1Alpha;
using GrpcSample.Domain.Auth;
using GrpcSample.Native.Contracts;
using Xunit;

namespace GrpcSample.Native.Tests;

public sealed class NativeGrpcIntegrationTests(NativeGrpcServerFixture fixture)
    : IClassFixture<NativeGrpcServerFixture>
{
    [Fact]
    public async Task HealthCheck_And_ServerReflection_AreServing()
    {
        var healthClient = new Health.HealthClient(fixture.Channel);
        var health = await healthClient.CheckAsync(new HealthCheckRequest());
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, health.Status);

        var reflectionClient = new ServerReflection.ServerReflectionClient(fixture.Channel);
        using var call = reflectionClient.ServerReflectionInfo();
        await call.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = "" });
        await call.RequestStream.CompleteAsync();

        var serviceNames = new List<string>();
        await foreach (var resp in call.ResponseStream.ReadAllAsync())
        {
            serviceNames.AddRange(resp.ListServicesResponse.Service.Select(s => s.Name));
        }
        Assert.Contains("commerce.v1.CommerceService", serviceNames);
    }

    [Fact]
    public async Task Unary_ProductCrud_List_And_Summary_WorkWithMetadataAndTrailers()
    {
        var client = fixture.CreateClient(ApiKeyConstants.AdminKey);
        var corrId = "nat-test-corr-001";
        var reqHeaders = new Metadata { { ApiKeyConstants.CorrelationIdHeader, corrId } };

        var getCall = client.GetProductAsync(new GetProductRequest { Id = "PRD-001" }, headers: reqHeaders);
        var p1 = await getCall.ResponseAsync;
        var respHeaders = await getCall.ResponseHeadersAsync;
        var trailers = getCall.GetTrailers();

        Assert.Equal("PRD-001", p1.Id);
        Assert.Equal("native-grpc", respHeaders.GetValue(ApiKeyConstants.ServerEngineHeader));
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
            Sku = "SKU-NAT-TEST-1",
            Name = "Native Test Item",
            Description = "Created in test",
            Category = ProductCategory.Gaming,
            Price = 99.95,
            StockQuantity = 30,
            Rating = 4.7,
            Tags = { "test" },
            Attributes = { ["env"] = "xunit" }
        });
        Assert.StartsWith("PRD-", created.Id);

        var updated = await client.UpdateProductAsync(new UpdateProductRequest
        {
            Id = created.Id,
            Name = "Native Test Item V2",
            Description = "Updated in test",
            Category = ProductCategory.Audio,
            Price = 119.95,
            StockQuantity = 25,
            IsActive = true,
            Tags = { "test", "v2" }
        });
        Assert.Equal("Native Test Item V2", updated.Name);
        Assert.Equal(ProductCategory.Audio, updated.Category);

        var deleted = await client.DeleteProductAsync(new DeleteProductRequest { Id = created.Id });
        Assert.True(deleted.Deleted);

        var summary = await client.GetCatalogSummaryAsync(new Empty());
        Assert.True(summary.TotalProducts >= 60);
        Assert.True(summary.TotalOrders >= 35);
    }

    [Fact]
    public async Task Unary_Orders_And_AdminAuditInfo_WorkEndToEnd()
    {
        var client = fixture.CreateClient(ApiKeyConstants.AdminKey);

        var seededOrder = await client.GetOrderAsync(new GetOrderRequest { Id = "ORD-1001" });
        Assert.Equal("ORD-1001", seededOrder.Id);

        var customerOrders = await client.ListOrdersByCustomerAsync(new ListOrdersByCustomerRequest { CustomerId = "CUS-001" });
        Assert.NotEmpty(customerOrders.Items);

        var createdOrder = await client.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "CUS-002",
            ShippingAddress = "42 gRPC Way, Ankara",
            Items = { new CreateOrderLineRequest { ProductId = "PRD-002", Quantity = 2 } }
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

        // Unauthenticated
        var exUnauth = await Assert.ThrowsAsync<RpcException>(async () =>
            await anonClient.CreateProductAsync(new CreateProductRequest { Sku = "X", Name = "X", Price = 10 }));
        Assert.Equal(StatusCode.Unauthenticated, exUnauth.StatusCode);

        // PermissionDenied for User role on Admin-only DeleteProduct & GetAdminAuditInfo
        var exForbidden = await Assert.ThrowsAsync<RpcException>(async () =>
            await userClient.DeleteProductAsync(new DeleteProductRequest { Id = "PRD-001" }));
        Assert.Equal(StatusCode.PermissionDenied, exForbidden.StatusCode);

        var exAuditForbidden = await Assert.ThrowsAsync<RpcException>(async () =>
            await userClient.GetAdminAuditInfoAsync(new Empty()));
        Assert.Equal(StatusCode.PermissionDenied, exAuditForbidden.StatusCode);

        // NotFound
        var exNotFound = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.GetProductAsync(new GetProductRequest { Id = "PRD-9999" }));
        Assert.Equal(StatusCode.NotFound, exNotFound.StatusCode);

        // AlreadyExists
        var exDup = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.CreateProductAsync(new CreateProductRequest
            {
                Sku = "SKU-ELEC-001",
                Name = "Duplicate",
                Price = 10,
                StockQuantity = 5
            }));
        Assert.Equal(StatusCode.AlreadyExists, exDup.StatusCode);

        // InvalidArgument
        var exInvalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.CreateProductAsync(new CreateProductRequest { Sku = "", Name = "", Price = -1 }));
        Assert.Equal(StatusCode.InvalidArgument, exInvalid.StatusCode);

        // FailedPrecondition (insufficient stock)
        var exPrecond = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.CreateOrderAsync(new CreateOrderRequest
            {
                CustomerId = "CUS-001",
                ShippingAddress = "Test",
                Items = { new CreateOrderLineRequest { ProductId = "PRD-001", Quantity = 50000 } }
            }));
        Assert.Equal(StatusCode.FailedPrecondition, exPrecond.StatusCode);

        // DeadlineExceeded
        var exDeadline = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.SimulateSlowOperationAsync(
                new SlowOperationRequest { DelayMs = 2000, Label = "timeout-test" },
                deadline: DateTime.UtcNow.AddMilliseconds(80)));
        Assert.Equal(StatusCode.DeadlineExceeded, exDeadline.StatusCode);

        // Client cancellation
        using var cts = new CancellationTokenSource(80);
        var exCancel = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.SimulateSlowOperationAsync(
                new SlowOperationRequest { DelayMs = 2000, Label = "cancel-test" },
                cancellationToken: cts.Token));
        Assert.Equal(StatusCode.Cancelled, exCancel.StatusCode);

        // Happy path slow operation
        var okSlow = await adminClient.SimulateSlowOperationAsync(new SlowOperationRequest { DelayMs = 15, Label = "fast" });
        Assert.Equal("native-grpc", okSlow.ServerEngine);
    }

    [Fact]
    public async Task ServerStreaming_Products_Telemetry_And_MidStreamCancellation()
    {
        var client = fixture.CreateClient();

        using var prodCall = client.StreamProducts(new StreamProductsRequest
        {
            Category = ProductCategory.Audio,
            ActiveOnly = true
        });
        var products = new List<ProductModel>();
        await foreach (var p in prodCall.ResponseStream.ReadAllAsync())
            products.Add(p);
        Assert.Equal(10, products.Count);

        // Mid-stream cancel
        using var cts = new CancellationTokenSource();
        using var slowStream = client.StreamProducts(new StreamProductsRequest { DelayMs = 30 }, cancellationToken: cts.Token);
        var partial = new List<ProductModel>();
        await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var p in slowStream.ResponseStream.ReadAllAsync(cts.Token))
            {
                partial.Add(p);
                if (partial.Count == 2) cts.Cancel();
            }
        });
        Assert.Equal(2, partial.Count);

        // Telemetry subscription
        using var telCall = client.SubscribeWarehouseTelemetry(new TelemetrySubscriptionRequest
        {
            WarehouseId = "WH-IST-01",
            AnomaliesOnly = true,
            MaxReadings = 10
        });
        var readings = new List<TelemetryReadingModel>();
        await foreach (var r in telCall.ResponseStream.ReadAllAsync())
            readings.Add(r);
        Assert.NotEmpty(readings);
        Assert.All(readings, r => Assert.True(r.IsAnomaly && r.WarehouseId == "WH-IST-01"));
    }

    [Fact]
    public async Task ClientStreaming_UploadBlock_IngestTelemetry_And_DownloadBlock_WithCompression()
    {
        var client = fixture.CreateClient(ApiKeyConstants.UserKey);

        // Client streaming upload with gzip compression metadata
        var payload = Encoding.UTF8.GetBytes(string.Join("-", Enumerable.Range(1, 1200).Select(i => $"native-block-{i}")));
        var gzipHeaders = new Metadata { { "grpc-internal-encoding-request", "gzip" } };

        using var uploadCall = client.UploadBlock(headers: gzipHeaders);
        await uploadCall.RequestStream.WriteAsync(new BlockUploadChunk
        {
            Header = new BlockHeader { BlockName = "integration-native.bin", ContentType = "application/octet-stream" }
        });
        for (int i = 0; i < payload.Length; i += 1024)
        {
            var slice = payload.AsMemory(i, Math.Min(1024, payload.Length - i));
            await uploadCall.RequestStream.WriteAsync(new BlockUploadChunk { ChunkData = UnsafeByteOperations.UnsafeWrap(slice) });
        }
        await uploadCall.RequestStream.CompleteAsync();
        var uploadResult = await uploadCall.ResponseAsync;

        Assert.Equal(payload.Length, uploadResult.TotalBytes);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(payload)), uploadResult.Sha256Hex);

        // Server streaming download + SHA-256 verification
        using var dlCall = client.DownloadBlock(new BlockDownloadRequest { BlockIdOrName = uploadResult.BlockId, ChunkSize = 1024 });
        using var ms = new MemoryStream();
        await foreach (var chunk in dlCall.ResponseStream.ReadAllAsync())
            chunk.ChunkData.WriteTo(ms);

        Assert.Equal(payload, ms.ToArray());

        // Client streaming telemetry ingestion
        using var telIngest = client.IngestTelemetryBatch();
        await telIngest.RequestStream.WriteAsync(new TelemetryReadingModel
        {
            WarehouseId = "WH-BER-04",
            SensorId = "SENS-99",
            TemperatureCelsius = 21.5,
            HumidityPercent = 44.0,
            CpuLoadPercent = 35.0
        });
        await telIngest.RequestStream.WriteAsync(new TelemetryReadingModel
        {
            WarehouseId = "WH-LON-05",
            SensorId = "SENS-98",
            TemperatureCelsius = 38.0,
            HumidityPercent = 55.0,
            CpuLoadPercent = 91.0
        });
        await telIngest.RequestStream.CompleteAsync();
        var ingestSummary = await telIngest.ResponseAsync;

        Assert.Equal(2, ingestSummary.AcceptedCount);
        Assert.Equal(1, ingestSummary.AnomalyCount);
        Assert.Equal(91.0, ingestSummary.MaxCpuLoadPercent);
    }

    [Fact]
    public async Task BidirectionalStreaming_LiveOrderChat_And_ProcessPriceQuotes()
    {
        var client = fixture.CreateClient();

        // LiveOrderChat
        using var chatCall = client.LiveOrderChat();
        var chatResponses = new List<ChatStreamResponse>();
        var readChat = Task.Run(async () =>
        {
            await foreach (var r in chatCall.ResponseStream.ReadAllAsync())
                chatResponses.Add(r);
        });

        await chatCall.RequestStream.WriteAsync(new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.Join });
        await chatCall.RequestStream.WriteAsync(new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.Ping });
        await chatCall.RequestStream.WriteAsync(new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.OrderStatusQuery, OrderId = "ORD-1001" });
        await chatCall.RequestStream.WriteAsync(new ChatStreamRequest { RoomId = "order-support", Sender = "eren", Command = ChatCommandType.Message, Text = "Checking order" });
        await chatCall.RequestStream.CompleteAsync();
        await readChat;

        Assert.Equal(4, chatResponses.Count);
        Assert.Contains(chatResponses, r => r.Text == "PONG");
        Assert.Contains(chatResponses, r => r.Text.Contains("ORD-1001"));
        Assert.Contains(chatResponses, r => r.Text.Contains("Echo[order-support]: Checking order"));

        // ProcessPriceQuotes
        using var quoteCall = client.ProcessPriceQuotes();
        var quotes = new List<PriceQuoteResponse>();
        var readQuotes = Task.Run(async () =>
        {
            await foreach (var q in quoteCall.ResponseStream.ReadAllAsync())
                quotes.Add(q);
        });

        await quoteCall.RequestStream.WriteAsync(new PriceQuoteRequest { Sku = "SKU-ELEC-001", Quantity = 2, CustomerTier = CustomerTier.Gold });
        await quoteCall.RequestStream.WriteAsync(new PriceQuoteRequest { Sku = "SKU-BOOK-001", Quantity = 10, CustomerTier = CustomerTier.Enterprise });
        await quoteCall.RequestStream.CompleteAsync();
        await readQuotes;

        Assert.Equal(2, quotes.Count);
        Assert.Equal(10, quotes[0].DiscountPercent);
        Assert.Equal(20, quotes[1].DiscountPercent);
        Assert.True(quotes[1].RunningSubtotal > quotes[0].RunningSubtotal);
    }

    [Fact]
    public async Task JsonTranscoding_HttpEndpoints_ReturnExpectedJson()
    {
        using var http = new HttpClient { BaseAddress = fixture.HttpUri };

        var productResp = await http.GetAsync("/v1/products/PRD-001");
        Assert.Equal(HttpStatusCode.OK, productResp.StatusCode);
        var productJson = await productResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(productJson);
        Assert.Equal("PRD-001", productJson!["id"].ToString());

        var summaryResp = await http.GetAsync("/v1/summary");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);

        var listResp = await http.GetAsync("/v1/products?page=1&pageSize=3");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
    }
}
