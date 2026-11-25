using System.IO.Compression;
using Grpc.AspNetCore.Server;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Chat;
using GrpcSample.Domain.Chat.Handlers;
using GrpcSample.Domain.Configuration;
using GrpcSample.Domain.Policies.Blocks;
using GrpcSample.Domain.Policies.Pricing;
using GrpcSample.Domain.Policies.Pricing.Rules;
using GrpcSample.Domain.Policies.Telemetry;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.InMemory;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Abstractions;
using GrpcSample.Domain.Services.Implementations;
using GrpcSample.Infrastructure.Grpc.Auth;
using GrpcSample.Infrastructure.Grpc.Configuration;
using GrpcSample.Infrastructure.Grpc.Errors;
using GrpcSample.Infrastructure.Grpc.Interceptors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcSample.Infrastructure.Grpc.Extensions;

public static class CommercePlatformServiceCollectionExtensions
{
    public static IServiceCollection AddCommerceDomain(
        this IServiceCollection services,
        Action<TelemetryAnomalyOptions>? configureTelemetry = null,
        Action<PricingOptions>? configurePricing = null)
    {
        var telemetryOptions = new TelemetryAnomalyOptions();
        configureTelemetry?.Invoke(telemetryOptions);

        var pricingOptions = new PricingOptions();
        configurePricing?.Invoke(pricingOptions);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(telemetryOptions);
        services.AddSingleton(pricingOptions);

        // Seeded in-memory backing store and ISP-segregated repositories
        services.AddSingleton(_ => InMemoryCommerceStore.CreateSeeded());
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<ITelemetryRepository, InMemoryTelemetryRepository>();
        services.AddSingleton<IBlockRepository, InMemoryBlockRepository>();
        services.AddSingleton<IChatRepository, InMemoryChatRepository>();
        services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();

        // OCP Strategies & Command Handlers
        services.AddSingleton<IDiscountRule, CustomerTierDiscountRule>();
        services.AddSingleton<IDiscountRule, VolumeDiscountRule>();
        services.AddSingleton<IPricingStrategy, TieredVolumePricingStrategy>();
        services.AddSingleton<ITelemetryAnomalyDetector, ThresholdTelemetryAnomalyDetector>();
        services.AddSingleton<IBlockChecksumCalculator, Sha256BlockChecksumCalculator>();

        services.AddSingleton<IChatCommandHandler, JoinChatCommandHandler>();
        services.AddSingleton<IChatCommandHandler, PingChatCommandHandler>();
        services.AddSingleton<IChatCommandHandler, OrderStatusChatCommandHandler>();
        services.AddSingleton<IChatCommandHandler, MessageChatCommandHandler>();
        services.AddSingleton<IOrderChatProcessor, OrderChatProcessor>();

        // Focused Application/Domain Services
        services.AddSingleton<IProductCatalogService, ProductCatalogService>();
        services.AddSingleton<IOrderProcessingService, OrderProcessingService>();
        services.AddSingleton<ITelemetryService, TelemetryService>();
        services.AddSingleton<IBlockStorageService, BlockStorageService>();
        services.AddSingleton<IAuditService, AuditService>();

        return services;
    }

    public static IServiceCollection AddCommerceGrpcInfrastructure(
        this IServiceCollection services,
        string engineName,
        Action<ApiKeyAuthOptions>? configureAuth = null)
    {
        var authOptions = new ApiKeyAuthOptions();
        configureAuth?.Invoke(authOptions);

        services.AddSingleton(new GrpcEngineOptions { EngineName = engineName });
        services.AddSingleton(authOptions);
        services.AddSingleton<IApiKeyValidator, OptionsApiKeyValidator>();
        services.AddSingleton<IGrpcExceptionMapper, GrpcExceptionMapper>();

        services
            .AddAuthentication(ApiKeyConstants.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyConstants.AuthenticationScheme, _ => { });
        services.AddAuthorization();

        services
            .AddGrpcHealthChecks()
            .AddCheck(engineName, () => HealthCheckResult.Healthy($"{engineName} server is healthy."));

        return services;
    }

    public static void ConfigureStandardGrpcOptions(GrpcServiceOptions options, bool addAuditInterceptor)
    {
        options.EnableDetailedErrors = true;
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.ResponseCompressionAlgorithm = "gzip";
        options.ResponseCompressionLevel = CompressionLevel.Fastest;

        if (addAuditInterceptor)
            options.Interceptors.Add<ServerAuditInterceptor>();
    }
}
