namespace GrpcSample.Domain.Services.Contracts;

public sealed record BlockChunkSlice(
    string BlockId,
    string BlockName,
    int Sequence,
    bool IsLast,
    ReadOnlyMemory<byte> Data,
    string ExpectedSha256Hex);
