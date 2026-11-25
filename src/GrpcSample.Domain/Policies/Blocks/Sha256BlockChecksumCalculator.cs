using System.Security.Cryptography;

namespace GrpcSample.Domain.Policies.Blocks;

public sealed class Sha256BlockChecksumCalculator : IBlockChecksumCalculator
{
    public string ComputeHex(ReadOnlySpan<byte> data) =>
        Convert.ToHexStringLower(SHA256.HashData(data));
}
