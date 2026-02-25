using System.Text;
using Stoat.Services.Enums;
using Stoat.Services.Interfaces;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;

namespace Stoat.Services.Services;

/// <summary>
/// Service for standalone hashing operations using BouncyCastle.
/// </summary>
public class HashService : IHashService
{
    public string HashText(string input, HashAlgorithmType algorithm)
    {
        var digest = CreateDigest(algorithm);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        digest.BlockUpdate(inputBytes, 0, inputBytes.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<string> HashFileAsync(string filePath, HashAlgorithmType algorithm,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var digest = CreateDigest(algorithm);
        var fileInfo = new FileInfo(filePath);
        var totalBytes = fileInfo.Length;
        var processedBytes = 0L;

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            digest.BlockUpdate(buffer, 0, bytesRead);
            processedBytes += bytesRead;
            progress?.Report((double)processedBytes / totalBytes * 100);
        }

        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool VerifyTextHash(string input, string expectedHash, HashAlgorithmType algorithm)
    {
        var computedHash = HashText(input, algorithm);
        return string.Equals(computedHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, HashAlgorithmType algorithm,
        CancellationToken ct = default)
    {
        var computedHash = await HashFileAsync(filePath, algorithm, ct: ct);
        return string.Equals(computedHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static IDigest CreateDigest(HashAlgorithmType algorithm) => algorithm switch
    {
        HashAlgorithmType.MD5 => new MD5Digest(),
        HashAlgorithmType.SHA1 => new Sha1Digest(),
        HashAlgorithmType.SHA224 => new Sha224Digest(),
        HashAlgorithmType.SHA256 => new Sha256Digest(),
        HashAlgorithmType.SHA384 => new Sha384Digest(),
        HashAlgorithmType.SHA512 => new Sha512Digest(),
        HashAlgorithmType.SHA3_256 => new Sha3Digest(256),
        HashAlgorithmType.SHA3_384 => new Sha3Digest(384),
        HashAlgorithmType.SHA3_512 => new Sha3Digest(512),
        HashAlgorithmType.BLAKE2b_256 => new Blake2bDigest(256),
        HashAlgorithmType.BLAKE2b_384 => new Blake2bDigest(384),
        HashAlgorithmType.BLAKE2b_512 => new Blake2bDigest(512),
        HashAlgorithmType.RIPEMD160 => new RipeMD160Digest(),
        HashAlgorithmType.Whirlpool => new WhirlpoolDigest(),
        HashAlgorithmType.SM3 => new SM3Digest(),
        HashAlgorithmType.Tiger => new TigerDigest(),
        _ => throw new ArgumentException($"Unsupported hash algorithm: {algorithm}")
    };
}
