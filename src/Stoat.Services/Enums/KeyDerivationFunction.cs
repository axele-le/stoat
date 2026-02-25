namespace Stoat.Services.Enums;

/// <summary>
/// Supported key derivation functions.
/// </summary>
public enum KeyDerivationFunction
{
    PBKDF2,
    Argon2id,
    Scrypt,
    HKDF
}
