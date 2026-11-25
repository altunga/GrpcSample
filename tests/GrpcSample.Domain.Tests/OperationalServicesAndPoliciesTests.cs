using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Chat.Handlers;
using GrpcSample.Domain.Configuration;
using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Policies.Blocks;
using GrpcSample.Domain.Policies.Telemetry;
using GrpcSample.Domain.Repositories.InMemory;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Implementations;
using GrpcSample.Infrastructure.Grpc.Errors;
using Xunit;

namespace GrpcSample.Domain.Tests;

public sealed class OperationalServicesAndPoliciesTests
{
    [Fact]
    public void TelemetryService_ClassifiesAnomaliesUsingConfigurableThresholds()
    {
        var store = InMemoryCommerceStore.CreateSeeded();
        var customDetector = new ThresholdTelemetryAnomalyDetector(new TelemetryAnomalyOptions
        {
            MaxTemperatureCelsius = 25.0,
            MaxCpuLoadPercent = 70.0
        });
        var service = new TelemetryService(new InMemoryTelemetryRepository(store), customDetector, TimeProvider.System);

        var summary = service.IngestBatch([
            new TelemetryReading { ReadingId = "", WarehouseId = "WH-IST-01", SensorId = "S1", TemperatureCelsius = 22.0, HumidityPercent = 40, CpuLoadPercent = 30 },
            new TelemetryReading { ReadingId = "", WarehouseId = "WH-ANK-02", SensorId = "S2", TemperatureCelsius = 26.5, HumidityPercent = 50, CpuLoadPercent = 60 }
        ]);

        Assert.Equal(2, summary.AcceptedCount);
        Assert.Equal(1, summary.AnomalyCount);
    }

    [Fact]
    public void BlockStorageService_SavesComputesSha256_AndSlicesChunks()
    {
        var store = InMemoryCommerceStore.CreateSeeded();
        var service = new BlockStorageService(
            new InMemoryBlockRepository(store),
            new Sha256BlockChecksumCalculator(),
            TimeProvider.System);

        var bytes = Encoding.UTF8.GetBytes(string.Join("|", Enumerable.Range(1, 200).Select(i => $"chunk-{i}")));
        var saved = service.SaveBlock("solid-test.bin", "application/octet-stream", bytes, 3);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), saved.Sha256Hex);

        var slices = service.SliceBlockChunks(saved, 512).ToList();
        Assert.True(slices.Count > 1);
        Assert.True(slices[^1].IsLast);
        Assert.Equal(bytes, slices.SelectMany(s => s.Data.ToArray()).ToArray());
    }

    [Fact]
    public void OrderChatProcessor_DispatchesAllCommands_AndSupportsExtension()
    {
        var store = InMemoryCommerceStore.CreateSeeded();
        var chatRepo = new InMemoryChatRepository(store);
        var orderRepo = new InMemoryOrderRepository(store);
        var processor = new OrderChatProcessor([
            new JoinChatCommandHandler(chatRepo, TimeProvider.System),
            new PingChatCommandHandler(TimeProvider.System),
            new OrderStatusChatCommandHandler(orderRepo, TimeProvider.System),
            new MessageChatCommandHandler(chatRepo, TimeProvider.System)
        ]);

        var joinReply = processor.Process(new ChatCommandInput("", "", ChatCommandKind.Join, "", null));
        Assert.Equal("order-support", joinReply.RoomId);
        Assert.Contains("Welcome guest", joinReply.Text);

        var pingReply = processor.Process(new ChatCommandInput("r1", "eren", ChatCommandKind.Ping, "", null));
        Assert.Equal("PONG", pingReply.Text);

        var orderReply = processor.Process(new ChatCommandInput("r1", "eren", ChatCommandKind.OrderStatusQuery, "", "ORD-1001"));
        Assert.Contains("ORD-1001", orderReply.Text);

        var msgReply = processor.Process(new ChatCommandInput("r1", "eren", ChatCommandKind.Message, "hello", null));
        Assert.Equal("Echo[r1]: hello", msgReply.Text);
    }

    [Fact]
    public void ApiKeyValidator_And_GrpcExceptionMapper_WorkAsSingleSourceOfTruth()
    {
        var validator = new OptionsApiKeyValidator(new ApiKeyAuthOptions());
        Assert.True(validator.TryValidate(ApiKeyConstants.AdminKey, out var admin));
        Assert.Equal("Admin", admin.Role);
        Assert.False(validator.TryValidate("wrong-key", out _));

        var mapper = new GrpcExceptionMapper();
        Assert.Equal(StatusCode.NotFound, mapper.ToRpcException(new DomainNotFoundException("missing")).StatusCode);
        Assert.Equal(StatusCode.AlreadyExists, mapper.ToRpcException(new DomainAlreadyExistsException("dup")).StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, mapper.ToRpcException(new DomainValidationException("bad")).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, mapper.ToRpcException(new DomainPreconditionFailedException("stock")).StatusCode);
        Assert.Equal(StatusCode.Cancelled, mapper.ToRpcException(new OperationCanceledException()).StatusCode);
    }
}
