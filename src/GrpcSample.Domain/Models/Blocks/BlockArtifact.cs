namespace GrpcSample.Domain.Models.Blocks;

public sealed class BlockArtifact
{
    public required string BlockId { get; set; }
    public required string BlockName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }
    public long TotalBytes { get; set; }
    public int ChunkCount { get; set; }
    public required string Sha256Hex { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
