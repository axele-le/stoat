using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Stoat.Services.Enums;

namespace Stoat.Converters;

/// <summary>
/// Converts enum values to user-friendly display strings.
/// </summary>
public class EnumDisplayConverter : IValueConverter
{
    public static readonly EnumDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            SymmetricCipher cipher => cipher switch
            {
                SymmetricCipher.AES => "AES",
                SymmetricCipher.Twofish => "Twofish",
                SymmetricCipher.Serpent => "Serpent",
                SymmetricCipher.Camellia => "Camellia",
                SymmetricCipher.ARIA => "ARIA",
                SymmetricCipher.SM4 => "SM4",
                SymmetricCipher.SEED => "SEED",
                SymmetricCipher.Blowfish => "Blowfish",
                SymmetricCipher.CAST5 => "CAST5",
                SymmetricCipher.CAST6 => "CAST6",
                SymmetricCipher.IDEA => "IDEA",
                SymmetricCipher.TripleDES => "3DES",
                SymmetricCipher.ChaCha20 => "ChaCha20-Poly1305",
                _ => value.ToString()
            },
            CipherBlockMode mode => mode switch
            {
                CipherBlockMode.CBC => "CBC",
                CipherBlockMode.CFB => "CFB",
                CipherBlockMode.CTR => "CTR",
                CipherBlockMode.OFB => "OFB",
                CipherBlockMode.ECB => "ECB",
                CipherBlockMode.GCM => "GCM",
                CipherBlockMode.CCM => "CCM",
                CipherBlockMode.EAX => "EAX",
                _ => value.ToString()
            },
            KeyDerivationFunction kdf => kdf switch
            {
                KeyDerivationFunction.PBKDF2 => "PBKDF2",
                KeyDerivationFunction.Argon2id => "Argon2id",
                KeyDerivationFunction.Scrypt => "Scrypt",
                KeyDerivationFunction.HKDF => "HKDF",
                _ => value.ToString()
            },
            KdfHashAlgorithm kdfHash => kdfHash switch
            {
                KdfHashAlgorithm.SHA1 => "SHA-1",
                KdfHashAlgorithm.SHA256 => "SHA-256",
                KdfHashAlgorithm.SHA384 => "SHA-384",
                KdfHashAlgorithm.SHA512 => "SHA-512",
                KdfHashAlgorithm.SHA3_256 => "SHA3-256",
                KdfHashAlgorithm.SHA3_384 => "SHA3-384",
                KdfHashAlgorithm.SHA3_512 => "SHA3-512",
                _ => value.ToString()
            },
            HashAlgorithmType hashType => hashType switch
            {
                HashAlgorithmType.MD5 => "MD5",
                HashAlgorithmType.SHA1 => "SHA-1",
                HashAlgorithmType.SHA224 => "SHA-224",
                HashAlgorithmType.SHA256 => "SHA-256",
                HashAlgorithmType.SHA384 => "SHA-384",
                HashAlgorithmType.SHA512 => "SHA-512",
                HashAlgorithmType.SHA3_256 => "SHA3-256",
                HashAlgorithmType.SHA3_384 => "SHA3-384",
                HashAlgorithmType.SHA3_512 => "SHA3-512",
                HashAlgorithmType.BLAKE2b_256 => "BLAKE2b-256",
                HashAlgorithmType.BLAKE2b_384 => "BLAKE2b-384",
                HashAlgorithmType.BLAKE2b_512 => "BLAKE2b-512",
                HashAlgorithmType.RIPEMD160 => "RIPEMD-160",
                HashAlgorithmType.Whirlpool => "Whirlpool",
                HashAlgorithmType.SM3 => "SM3",
                HashAlgorithmType.Tiger => "Tiger",
                _ => value.ToString()
            },
            int iterations => iterations switch
            {
                10000 => "10.000",
                50000 => "50.000",
                100000 => "100.000",
                250000 => "250.000",
                500000 => "500.000",
                1000000 => "1.000.000",
                _ => iterations.ToString("N0")
            },
            _ => value?.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
