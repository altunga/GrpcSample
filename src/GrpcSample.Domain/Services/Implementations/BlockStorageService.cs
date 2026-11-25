using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models.Blocks;
using GrpcSample.Domain.Policies.Blocks;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Services.Abstractions;
using GrpcSample.Domain.Services.Contracts;

namespace GrpcSample.Domain.Services.Implementations;

public sealed class BlockStorageService(
    IBlockRepository blockRepository,
    IBlockChecksumCalculator checksumCalculator,
    TimeProvider timeProvider) : IBlockStorageService
{
    public BlockArtifact GetBlock(string blockIdOrName) => blockRepository.GetByIdOrName(blockIdOrName);

    public IReadOnlyList<BlockArtifact> ListBlocks() => blockRepository.List();

    public BlockArtifact SaveBlock(string blockName, string contentType, byte[] data, int chunkCount)
    {
        if (string.IsNullOrWhiteSpace(blockName))
            throw new DomainValidationException("BlockName is required.");
        if (data.Length == 0)
            throw new DomainValidationException("Block payload cannot be empty.");

        var sha256Hex = checksumCalculator.ComputeHex(data);
        return blockRepository.Save(
            blockName,
            contentType,
            data,
            chunkCount,
            sha256Hex,
            timeProvider.GetUtcNow().UtcDateTime);
    }

    public IEnumerable<BlockChunkSlice> SliceBlockChunks(BlockArtifact block, int requestedChunkSize)
    {
        int chunkSize = Math.Clamp(requestedChunkSize <= 0 ? 4096 : requestedChunkSize, 256, 1024 * 1024);
        int seq = 0;

        for (int offset = 0; offset < block.Data.Length; offset += chunkSize)
        {
            int len = Math.Min(chunkSize, block.Data.Length - offset);
            bool isLast = offset + len >= block.Data.Length;
            yield return new BlockChunkSlice(
                block.BlockId,
                block.BlockName,
                seq++,
                isLast,
                block.Data.AsMemory(offset, len),
                block.Sha256Hex);
        }
    }
}
