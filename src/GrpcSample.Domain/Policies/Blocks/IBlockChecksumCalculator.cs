namespace GrpcSample.Domain.Policies.Blocks;

public interface IBlockChecksumCalculator
{
    string ComputeHex(ReadOnlySpan<byte> data);
}
