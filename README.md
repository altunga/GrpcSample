# GrpcSample — .NET 11 Comprehensive gRPC Showcase

[![.NET 11 CI](https://github.com/altunga/GrpcSample/actions/workflows/ci.yml/badge.svg)](https://github.com/altunga/GrpcSample/actions/workflows/ci.yml)
![.NET 11](https://img.shields.io/badge/.NET-11.0-512BD4)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A comprehensive **.NET 11 (`net11.0`)** gRPC reference solution implementing the same **Commerce, Warehouse Telemetry, and Binary Block Streaming** workload across three distinct gRPC approaches in .NET:

1. **`01-NativeGrpc`** — Proto-first official gRPC (`Grpc.AspNetCore` + `.proto` contracts + JSON Transcoding + Server Reflection + Health Checks)
2. **`02-CodeFirstGrpc`** — Code-first gRPC for .NET (`protobuf-net.Grpc` + C# `[Service]`/`[DataContract]` contracts + `IAsyncEnumerable<T>` streaming + runtime `/schema.proto` export)
3. **`03-MagicOnion`** — MagicOnion v7 (`MagicOnion.Server` / `MagicOnion.Client` + MessagePack serialization + `IService<T>` + real-time pub/sub `StreamingHub`)

---

## Solution Structure

```text
GrpcSample/
├── Directory.Build.props                     # Shared net11.0, Nullable, ImplicitUsings settings
├── Directory.Packages.props                  # Central Package Management (CPM)
├── GrpcSample.sln                            # Solution containing all 16 projects
├── README.md
├── src/
│   ├── GrpcSample.Domain/                    # Shared domain models, deterministic seed data, and business services
│   ├── GrpcSample.Infrastructure.Grpc/       # Shared ASP.NET Core gRPC server interceptors, auth handler, error mapping, health checks
│   ├── GrpcSample.Infrastructure.GrpcClient/ # Shared gRPC client correlation interceptor and retry channel factory
│   ├── 01-NativeGrpc/
│   │   ├── Protos/                           # common.proto, commerce.proto, google/api/annotations.proto & http.proto
│   │   ├── GrpcSample.Native.Contracts/      # Generated Protobuf server & client stubs
│   │   ├── GrpcSample.Native.Server/         # Native gRPC server (5101 HTTP/2 gRPC, 5100 HTTP/1.1 JSON Transcoding)
│   │   └── GrpcSample.Native.Client/         # Native gRPC sample client (Retry Policy, Interceptor, Reflection, Health, REST)
│   ├── 02-CodeFirstGrpc/
│   │   ├── GrpcSample.CodeFirst.Contracts/   # C# [Service] and [DataContract] service contracts & models
│   │   ├── GrpcSample.CodeFirst.Server/      # protobuf-net.Grpc server + /schema.proto (5201 HTTP/2 gRPC, 5200 HTTP/1.1)
│   │   └── GrpcSample.CodeFirst.Client/      # Code-first sample client (ClientFactory DI + CreateGrpcService + SchemaGenerator)
│   └── 03-MagicOnion/
│       ├── GrpcSample.MagicOnion.Shared/     # [MessagePackObject] models, ICommerceMagicService, ICommerceHub
│       ├── GrpcSample.MagicOnion.Server/     # MagicOnion v7 server + StreamingHub (5301 HTTP/2 gRPC, 5300 HTTP/1.1)
│       └── GrpcSample.MagicOnion.Client/     # MagicOnion sample client (IClientFilter + StreamingHubClient)
└── tests/
    ├── GrpcSample.Domain.Tests/              # Unit tests for seed data, catalog, orders, telemetry, blocks, and chat commands
    ├── GrpcSample.Native.Tests/              # Integration tests for all 18 RPCs, Reflection, Health, JSON Transcoding, and errors
    ├── GrpcSample.CodeFirst.Tests/           # Integration tests for all 18 RPCs, SchemaGenerator, Reflection, Health, and metadata
    └── GrpcSample.MagicOnion.Tests/          # Integration tests for all 18 RPCs, filters, Health, and multi-client StreamingHub
```

---

## Comparison of the Three gRPC Implementations

| Feature | `01-NativeGrpc` | `02-CodeFirstGrpc` (`protobuf-net.Grpc`) | `03-MagicOnion` v7 |
| :--- | :--- | :--- | :--- |
| **Contract Definition** | `.proto` (IDL) files | C# `interface` + `[Service]` / `[DataContract]` | C# `interface` (`IService<T>`, `IStreamingHub`) |
| **Wire Serialization** | Protobuf (`Google.Protobuf`) | Protobuf (`protobuf-net`) | MessagePack (`MessagePack-CSharp`) |
| **Cross-Language Support** | Native multi-language (Go, Rust, Python, Java, TS) | Exportable `.proto` via `SchemaGenerator` | .NET and Unity clients |
| **Streaming API** | `IAsyncStreamReader<T>` / `IServerStreamWriter<T>` | `IAsyncEnumerable<T>` | `ServerStreamingResult`, `ClientStreamingResult`, `DuplexStreamingResult`, `StreamingHub` |
| **Cross-Cutting Hooks** | gRPC `Interceptor` | gRPC `Interceptor` | `MagicOnionFilterAttribute` & `IClientFilter` |
| **Notable Extras** | gRPC JSON Transcoding (`/v1/...`), standard Reflection | Zero `.proto` authoring + live `/schema.proto` endpoint | High-speed MessagePack + room-based `StreamingHub` (`IGroup<TReceiver>`) |
| **Default Ports** | `5101` (HTTP/2 gRPC) / `5100` (HTTP/1.1 REST) | `5201` (HTTP/2 gRPC) / `5200` (HTTP/1.1 Schema) | `5301` (HTTP/2 gRPC) / `5300` (HTTP/1.1 Info) |

---

## Demonstrated gRPC Features (18 Shared RPCs + Platform Capabilities)

### 1. Unary RPCs (11 Operations)
- `GetProduct`, `ListProducts` (category, search term, active-only, max price, pagination), `CreateProduct`, `UpdateProduct`, `DeleteProduct`
- `GetOrder`, `ListOrdersByCustomer`, `CreateOrder` (atomic multi-item stock reservation)
- `GetCatalogSummary`, `GetAdminAuditInfo` (Admin-only request audit log)
- `SimulateSlowOperation` (used to demonstrate gRPC Deadlines and client `CancellationToken`)

### 2. Server Streaming RPCs (3 Operations)
- `StreamProducts` — Streams filtered catalog products; supports mid-stream cancellation via `CancellationToken`.
- `SubscribeWarehouseTelemetry` — Streams warehouse sensor telemetry with optional anomaly filtering (`IsAnomaly`).
- `DownloadBlock` — Streams binary payloads in configurable chunk sizes with SHA-256 checksum verification.

### 3. Client Streaming RPCs (2 Operations)
- `UploadBlock` — Streams binary chunks (plus header metadata / `oneof` in Native proto) from client to server, reassembles the payload, and computes/persists its SHA-256 hash.
- `IngestTelemetryBatch` — Accepts a stream of telemetry readings and returns batch statistics (accepted count, anomaly count, average temperature, max CPU load, affected warehouses).

### 4. Bidirectional (Duplex) Streaming RPCs (2 Operations)
- `LiveOrderChat` — Interactive command and message stream (`Join`, `Ping`, `OrderStatusQuery`, `Message`).
- `ProcessPriceQuotes` — Streams line-item quote requests and replies in real time with customer tier discounts, volume discounts, and running subtotal.

### 5. Cross-Cutting gRPC Capabilities
- **Metadata, Headers & Trailers**: Client correlation ID propagation (`x-correlation-id`), server engine response header (`x-server-engine`), processing duration trailer (`x-processing-time-ms`), item count trailer (`x-items-processed`), and request status trailer (`x-request-status`).
- **Standard gRPC Status Codes**: Consistent mapping to `NotFound`, `AlreadyExists`, `InvalidArgument`, `FailedPrecondition`, `Unauthenticated`, `PermissionDenied`, `DeadlineExceeded`, and `Cancelled`.
- **Authentication & Role Authorization**: `x-api-key` header authentication (`Admin`, `User`, `ReadOnly` roles).
- **Compression**: `gzip` response compression and client request compression (`grpc-internal-encoding-request: gzip`).
- **Retry Policy**: Client `ServiceConfig` / `MethodConfig` exponential backoff retries on `StatusCode.Unavailable`.
- **Health Checks & Reflection**: Standard `grpc.health.v1.Health` across all three servers; `ServerReflection` on Native and Code-First servers.
- **Native gRPC JSON Transcoding**: HTTP/1.1 REST endpoints (`/v1/products`, `/v1/products/{id}`, `/v1/orders/{id}`, `/v1/summary`).
- **Code-First `.proto` Schema Export**: Runtime `.proto` generation via `SchemaGenerator` exposed at `GET /schema.proto`.
- **MagicOnion v7 `StreamingHub`**: Real-time room-based pub/sub (`ICommerceHub` / `ICommerceHubReceiver`) broadcasting user join/leave, chat messages, and order status updates.

---

## Seed Data & Demo API Keys

Every server boots with a deterministic in-memory dataset:
- **60 Products** (`PRD-001` .. `PRD-060`) across 6 categories (`Electronics`, `Books`, `HomeOffice`, `Gaming`, `Audio`, `Accessories`; `PRD-059` is inactive)
- **25 Customers** (`CUS-001` .. `CUS-025`) across 4 tiers (`Standard`, `Silver`, `Gold`, `Enterprise`)
- **35 Orders** (`ORD-1001` .. `ORD-1035`) covering all order statuses (`Pending`, `Processing`, `Shipped`, `Delivered`, `Cancelled`)
- **120 Telemetry Readings** (`TEL-0001` .. `TEL-0120`) across 5 warehouses (`WH-IST-01`, `WH-ANK-02`, `WH-IZM-03`, `WH-BER-04`, `WH-LON-05`), including 20 deterministic anomalies
- **16 Binary Blocks** (`BLK-001` .. `BLK-016`, 4 KB–16 KB each) with SHA-256 checksums
- **3 Chat Rooms** (`order-support`, `warehouse-ops`, `vip-desk`) with seeded messages

### Demo API Keys (`x-api-key` header)

| API Key | Identity | Role | Access |
| :--- | :--- | :--- | :--- |
| `demo-admin-key` | `admin@grpcsample.dev` | `Admin` | All RPCs including `DeleteProduct` and `GetAdminAuditInfo` |
| `demo-user-key` | `operator@grpcsample.dev` | `User` | Product create/update, order creation, all read/streaming RPCs |
| `demo-readonly-key` | `viewer@grpcsample.dev` | `ReadOnly` | Authenticated read/streaming RPCs |

---

## Getting Started

### Prerequisites
- **.NET 11 SDK** (`11.0.100` or later)

### Build & Run All Tests

```bash
dotnet build GrpcSample.sln --configuration Release
dotnet test GrpcSample.sln --configuration Release --verbosity normal
```

### Run Each Server & Client Pair

#### 1. Native Proto-First gRPC
```bash
# Terminal 1 (gRPC on :5101, JSON Transcoding on :5100)
dotnet run --project src/01-NativeGrpc/GrpcSample.Native.Server

# Terminal 2
dotnet run --project src/01-NativeGrpc/GrpcSample.Native.Client

# Optional REST JSON Transcoding check
curl http://localhost:5100/v1/products/PRD-001
```

#### 2. Code-First gRPC (`protobuf-net.Grpc`)
```bash
# Terminal 1 (gRPC on :5201, /schema.proto on :5200)
dotnet run --project src/02-CodeFirstGrpc/GrpcSample.CodeFirst.Server

# Terminal 2
dotnet run --project src/02-CodeFirstGrpc/GrpcSample.CodeFirst.Client

# Optional runtime .proto inspection
curl http://localhost:5200/schema.proto
```

#### 3. MagicOnion v7
```bash
# Terminal 1 (gRPC + StreamingHub on :5301, HTTP info on :5300)
dotnet run --project src/03-MagicOnion/GrpcSample.MagicOnion.Server

# Terminal 2
dotnet run --project src/03-MagicOnion/GrpcSample.MagicOnion.Client
```
