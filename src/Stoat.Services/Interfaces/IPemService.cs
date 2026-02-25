using Stoat.Services.Models;

namespace Stoat.Services.Interfaces;

/// <summary>
/// Service for generating and validating RSA PEM key pairs.
/// </summary>
public interface IPemService
{
    /// <summary>
    /// Generates a new RSA key pair in PEM format.
    /// </summary>
    /// <param name="keySize">RSA key size in bits (default: 4096).</param>
    /// <returns>PEMs object containing private and public keys.</returns>
    PEMs GenerateKeyPair(int keySize = 4096);

    /// <summary>
    /// Validates that a PEM string is a valid public key.
    /// </summary>
    /// <param name="pemPublicKey">The PEM-encoded public key.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidatePublicKey(string pemPublicKey);

    /// <summary>
    /// Validates that a PEM string is a valid private key.
    /// </summary>
    /// <param name="pemPrivateKey">The PEM-encoded private key.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidatePrivateKey(string pemPrivateKey);

    /// <summary>
    /// Extracts the public key from a private key PEM.
    /// </summary>
    /// <param name="pemPrivateKey">The PEM-encoded private key.</param>
    /// <returns>The PEM-encoded public key, or null if invalid.</returns>
    string? ExtractPublicKeyFromPrivate(string pemPrivateKey);
}
