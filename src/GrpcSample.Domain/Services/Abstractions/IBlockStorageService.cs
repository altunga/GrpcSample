using GrpcSample.Domain.Models.Blocks;
using GrpcSample.Domain.Services.Contracts;

namespace GrpcSample.Domain.Services.Abstractions;

public interface IBlockStorageService
{
    BlockArtifact GetBlock(string blockIdOrName);
    IReadOnlyList<BlockArtifact> ListBlocks();
    BlockArtifact SaveBlock(string blockName, string contentType, byte[] data, int chunkCount);
    IEnumerable<BlockChunkSlice> SliceBlockChunks(BlockArtifact block, int requestedChunkSize);
}
