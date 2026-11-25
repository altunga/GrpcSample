using GrpcSample.Domain.Models.Blocks;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface IBlockRepository
{
    BlockArtifact GetByIdOrName(string blockIdOrName);
    IReadOnlyList<BlockArtifact> List();
    BlockArtifact Save(
        string blockName,
        string contentType,
        byte[] data,
        int chunkCount,
        string sha256Hex,
        DateTime uploadedAtUtc);
    int Count();
}
