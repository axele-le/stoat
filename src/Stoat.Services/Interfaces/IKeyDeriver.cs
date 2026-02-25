namespace Stoat.Services.Interfaces;

/// <summary>
/// Interface for key derivation operations.
/// </summary>
public interface IKeyDeriver
{
    /// <summary>
    /// Derives a key and IV from a password and salt.
    /// </summary>
    /// <param name="password">The password bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="keySizeBytes">The desired key size in bytes.</param>
    /// <param name="ivSizeBytes">The desired IV size in bytes.</param>
    /// <returns>A tuple containing the derived key and IV.</returns>
    (byte[] Key, byte[] Iv) DeriveKey(byte[] password, byte[] salt, int keySizeBytes, int ivSizeBytes);
}
