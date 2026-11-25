using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Health.V1;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Models.Blocks;
using GrpcSample.MagicOnion.Shared.Models.Chat;
using GrpcSample.MagicOnion.Shared.Models.Orders;
using GrpcSample.MagicOnion.Shared.Models.Products;
using GrpcSample.MagicOnion.Shared.Models.Telemetry;
using GrpcSample.MagicOnion.Shared.Hubs;
using GrpcSample.MagicOnion.Tests.TestDoubles;
using MagicOnion.Client;
using Xunit;

namespace GrpcSample.MagicOnion.Tests;

public sealed class MagicOnionIntegrationTests(MagicOnionServerFixture fixture)
    : IClassFixture<MagicOnionServerFixture>
{
    [Fact]
    public async Task HealthCheck_IsServing()
    {
        var health = await new Health.HealthClient(fixture.Channel).CheckAsync(new HealthCheckRequest());
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, health.Status);
    }

    [Fact]
    public async Task Unary_ProductCrud_List_And_Summary_WorkWithMetadataAndTrailers()
    {
        var client = fixture.CreateClient(ApiKeyConstants.AdminKey);
        var corrId = "mo-test-corr-001";

        var getCall = client
            .WithHeaders(new Metadata { { ApiKeyConstants.CorrelationIdHeader, corrId } })
            .GetProductAsync("PRD-001");
        var p1 = await getCall.ResponseAsync;
        var respHeaders = await getCall.ResponseHeadersAsync;
        var trailers = getCall.GetTrailers();

        Assert.Equal("PRD-001", p1.Id);
        Assert.Equal("magiconion", respHeaders.GetValue(ApiKeyConstants.ServerEngineHeader));
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
            Sku = "SKU-MO-TEST-1",
            Name = "MagicOnion Test Item",
            Description = "Created in test",
            Category = ProductCategory.Gaming,
            Price = 149.90m,
            StockQuantity = 35,
            Rating = 4.9,
            Tags = ["magiconion"],
            Attributes = new Dictionary<string, string> { ["env"] = "xunit" }
        });
        Assert.StartsWith("PRD-", created.Id);

        var updated = await client.UpdateProductAsync(new UpdateProductRequest
        {
            Id = created.Id,
            Name = "MagicOnion Test Item V2",
            Description = "Updated",
            Category = ProductCategory.Audio,
            Price = 169.90m,
            StockQuantity = 20,
            IsActive = true
        });
        Assert.Equal("MagicOnion Test Item V2", updated.Name);
        Assert.Equal(ProductCategory.Audio, updated.Category);

        var deleted = await client.DeleteProductAsync(created.Id);
        Assert.True(deleted.Deleted);

        var summary = await client.GetCatalogSummaryAsync();
        Assert.True(summary.TotalProducts >= 60);
    }

    [Fact]
    public async Task Unary_Orders_And_AdminAuditInfo_WorkEndToEnd()
    {
        var client = fixture.CreateClient(ApiKeyConstants.AdminKey);

        var seededOrder = await client.GetOrderAsync("ORD-1001");
        Assert.Equal("ORD-1001", seededOrder.Id);

        var customerOrders = await client.ListOrdersByCustomerAsync("CUS-001");
        Assert.NotEmpty(customerOrders);

        var createdOrder = await client.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "CUS-004",
            ShippingAddress = "99 MagicOnion Blvd, Berlin",
            Items = [new CreateOrderLineRequest { ProductId = "PRD-004", Quantity = 2 }]
        });
        Assert.StartsWith("ORD-", createdOrder.Id);
        Assert.Equal(OrderStatus.Pending, createdOrder.Status);

        var audit = await client.GetAdminAuditInfoAsync();
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
            await userClient.DeleteProductAsync("PRD-001"));
        Assert.Equal(StatusCode.PermissionDenied, exForbidden.StatusCode);

        var exAuditForbidden = await Assert.ThrowsAsync<RpcException>(async () =>
            await userClient.GetAdminAuditInfoAsync());
        Assert.Equal(StatusCode.PermissionDenied, exAuditForbidden.StatusCode);

        var exNotFound = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient.GetProductAsync("PRD-9999"));
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
            await adminClient
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(80))
                .SimulateSlowOperationAsync(2000, "timeout"));
        Assert.Equal(StatusCode.DeadlineExceeded, exDeadline.StatusCode);

        using var cts = new CancellationTokenSource(80);
        var exCancel = await Assert.ThrowsAsync<RpcException>(async () =>
            await adminClient
                .WithCancellationToken(cts.Token)
                .SimulateSlowOperationAsync(2000, "cancel"));
        Assert.Equal(StatusCode.Cancelled, exCancel.StatusCode);

        var okSlow = await adminClient.SimulateSlowOperationAsync(15, "ok");
        Assert.Equal("magiconion", okSlow.ServerEngine);
    }

    [Fact]
    public async Task ServerStreaming_Products_Telemetry_And_MidStreamCancellation()
    {
        var client = fixture.CreateClient();

        using var prodCall = await client.StreamProductsAsync(new StreamProductsRequest
        {
            Category = ProductCategory.Audio,
            ActiveOnly = true
        });
        var products = new List<ProductModel>();
        await foreach (var p in prodCall.ResponseStream.ReadAllAsync())
            products.Add(p);
        Assert.Equal(10, products.Count);

        using var cts = new CancellationTokenSource();
        using var slowCall = await client
            .WithCancellationToken(cts.Token)
            .StreamProductsAsync(new StreamProductsRequest { DelayMs = 30 });
        var partial = new List<ProductModel>();
        await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var p in slowCall.ResponseStream.ReadAllAsync(cts.Token))
            {
                partial.Add(p);
                if (partial.Count == 2) cts.Cancel();
            }
        });
        Assert.Equal(2, partial.Count);

        using var telCall = await client.SubscribeWarehouseTelemetryAsync(new TelemetrySubscriptionRequest
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
        var payload = Encoding.UTF8.GetBytes(string.Join("-", Enumerable.Range(1, 1200).Select(i => $"mo-block-{i}")));

        using var uploadCall = await client
            .WithHeaders(new Metadata { { "grpc-internal-encoding-request", "gzip" } })
            .UploadBlockAsync();

        await uploadCall.RequestStream.WriteAsync(new BlockUploadChunk
        {
            BlockName = "integration-mo.bin",
            ContentType = "application/octet-stream"
        });
        for (int i = 0; i < payload.Length; i += 1024)
        {
            var len = Math.Min(1024, payload.Length - i);
            await uploadCall.RequestStream.WriteAsync(new BlockUploadChunk
            {
                ChunkData = payload.AsSpan(i, len).ToArray()
            });
        }
        await uploadCall.RequestStream.CompleteAsync();
        var uploadResult = await uploadCall.ResponseAsync;

        Assert.Equal(payload.Length, uploadResult.TotalBytes);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(payload)), uploadResult.Sha256Hex);

        using var dlCall = await client.DownloadBlockAsync(uploadResult.BlockId, 1024);
        using var ms = new MemoryStream();
        await foreach (var chunk in dlCall.ResponseStream.ReadAllAsync())
            ms.Write(chunk.ChunkData, 0, chunk.ChunkData.Length);

        Assert.Equal(payload, ms.ToArray());

        using var telIngest = await client.IngestTelemetryBatchAsync();
        await telIngest.RequestStream.WriteAsync(new TelemetryReadingModel
        {
            WarehouseId = "WH-BER-04",
            SensorId = "S1",
            TemperatureCelsius = 20.5,
            HumidityPercent = 41,
            CpuLoadPercent = 32
        });
        await telIngest.RequestStream.WriteAsync(new TelemetryReadingModel
        {
            WarehouseId = "WH-LON-05",
            SensorId = "S2",
            TemperatureCelsius = 37.5,
            HumidityPercent = 52,
            CpuLoadPercent = 94
        });
        await telIngest.RequestStream.CompleteAsync();
        var ingestSummary = await telIngest.ResponseAsync;

        Assert.Equal(2, ingestSummary.AcceptedCount);
        Assert.Equal(1, ingestSummary.AnomalyCount);
        Assert.Equal(94.0, ingestSummary.MaxCpuLoadPercent);
    }

    [Fact]
    public async Task BidirectionalStreaming_LiveOrderChat_And_ProcessPriceQuotes()
    {
        var client = fixture.CreateClient();

        using var chatCall = await client.LiveOrderChatAsync();
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

        using var quoteCall = await client.ProcessPriceQuotesAsync();
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
        Assert.Equal(10m, quotes[0].DiscountPercent);
        Assert.Equal(20m, quotes[1].DiscountPercent);
        Assert.True(quotes[1].RunningSubtotal > quotes[0].RunningSubtotal);
    }

    [Fact]
    public async Task StreamingHub_MultiClient_BroadcastsJoinChatOrderUpdateAndLeave()
    {
        var receiverA = new TestHubReceiver();
        var receiverB = new TestHubReceiver();

        var hubA = await StreamingHubClient.ConnectAsync<ICommerceHub, ICommerceHubReceiver>(fixture.Channel, receiverA);
        var hubB = await StreamingHubClient.ConnectAsync<ICommerceHub, ICommerceHubReceiver>(fixture.Channel, receiverB);

        var snapA = await hubA.JoinRoomAsync("vip-desk", "alice");
        var snapB = await hubB.JoinRoomAsync("vip-desk", "bob");

        Assert.Equal("vip-desk", snapA.RoomName);
        Assert.Equal("bob", snapB.JoinedUser);

        await hubA.SendMessageAsync("Hello Bob from MagicOnion v7!");
        await hubB.BroadcastOrderUpdateAsync("ORD-1002", "Delivered");

        await receiverA.WaitForAsync(() =>
            receiverA.JoinedUsers.Contains("bob") &&
            receiverA.Messages.Any(m => m.Contains("Hello Bob")) &&
            receiverA.OrderUpdates.Any(u => u.Contains("ORD-1002->Delivered")));

        await receiverB.WaitForAsync(() =>
            receiverB.Messages.Any(m => m.Contains("Hello Bob")) &&
            receiverB.OrderUpdates.Any(u => u.Contains("ORD-1002->Delivered")));

        await hubB.LeaveRoomAsync();
        await receiverA.WaitForAsync(() => receiverA.LeftUsers.Contains("bob"));

        await hubA.LeaveRoomAsync();
        await hubA.DisposeAsync();
        await hubB.DisposeAsync();
    }
}
