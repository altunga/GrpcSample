using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Blocks;
using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryBlockRepository(InMemoryCommerceStore store) : IBlockRepository
{
    public BlockArtifact GetByIdOrName(string blockIdOrName)
    {
        if (string.IsNullOrWhiteSpace(blockIdOrName))
            throw new DomainValidationException("Block ID or name is required.");

        var key = blockIdOrName.Trim();
        if (store.Blocks.TryGetValue(key, out var byId))
            return byId;

        var byName = store.Blocks.Values
            .OrderByDescending(b => b.UploadedAtUtc)
            .FirstOrDefault(b => string.Equals(b.BlockName, key, StringComparison.OrdinalIgnoreCase));

        return byName ?? throw new DomainNotFoundException($"Block '{blockIdOrName}' was not found.");
    }

    public IReadOnlyList<BlockArtifact> List() =>
        store.Blocks.Values.OrderBy(b => b.BlockId, StringComparer.Ordinal).ToList();

    public BlockArtifact Save(
        string blockName,
        string contentType,
        byte[] data,
        int chunkCount,
        string sha256Hex,
        DateTime uploadedAtUtc)
    {
        var id = store.NextBlockId();
        var artifact = new BlockArtifact
        {
            BlockId = id,
            BlockName = blockName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            Data = data,
            TotalBytes = data.LongLength,
            ChunkCount = Math.Max(1, chunkCount),
            Sha256Hex = sha256Hex,
            UploadedAtUtc = uploadedAtUtc
        };

        store.Blocks[id] = artifact;
        return artifact;
    }

    public int Count() => store.Blocks.Count;
}
