using Stoat.Services.Exceptions;
using Stoat.Services.Interfaces;
using Stoat.Services.Models;
using Stoat.Services.Utils;

namespace Stoat.Services.Services;

/// <summary>
/// Service for encryption/decryption operations using the active profile's credentials.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ProfileService _profileService;

    public EncryptionService(ProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext, EncryptionSettings settings)
    {
        var credentials = _profileService.GetActiveCredentials()
            ?? throw new NoActiveProfileException();
        return SymmetricEncryption.Encrypt(credentials.secret, credentials.salt, plaintext, settings);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext, EncryptionSettings settings)
    {
        var credentials = _profileService.GetActiveCredentials()
            ?? throw new NoActiveProfileException();
        return SymmetricEncryption.Decrypt(credentials.secret, credentials.salt, ciphertext, settings);
    }

    /// <inheritdoc />
    public bool TryEncrypt(string plaintext, out string? ciphertext, out string? error, EncryptionSettings settings)
    {
        ciphertext = null;
        error = null;
        try
        {
            ciphertext = Encrypt(plaintext, settings);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryDecrypt(string ciphertext, out string? plaintext, out string? error, EncryptionSettings settings)
    {
        plaintext = null;
        error = null;
        try
        {
            plaintext = Decrypt(ciphertext, settings);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> EncryptFileAsync(string inputPath, string? outputPath = null,
        EncryptionSettings? settings = null, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var credentials = _profileService.GetActiveCredentials()
            ?? throw new NoActiveProfileException();

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Il file da criptare non esiste", inputPath);

        settings ??= EncryptionSettings.Default;
        outputPath ??= inputPath + SymmetricEncryption.EncryptedFileExtension;

        await SymmetricEncryption.EncryptFileAsync(credentials.secret, credentials.salt, inputPath, outputPath, settings, progress, ct);
        return outputPath;
    }

    /// <inheritdoc />
    public async Task<string> DecryptFileAsync(string inputPath, string? outputDirectory = null,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var credentials = _profileService.GetActiveCredentials()
            ?? throw new NoActiveProfileException();

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Il file da decriptare non esiste", inputPath);

        outputDirectory ??= Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
        return await SymmetricEncryption.DecryptFileAsync(credentials.secret, credentials.salt, inputPath, outputDirectory, progress, ct);
    }

    /// <inheritdoc />
    public async Task<(bool success, string? resultPath, string? error)> TryEncryptFileAsync(
        string inputPath, string? outputPath = null, EncryptionSettings? settings = null,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            var result = await EncryptFileAsync(inputPath, outputPath, settings, progress, ct);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, string? resultPath, string? error)> TryDecryptFileAsync(
        string inputPath, string? outputDirectory = null,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            var result = await DecryptFileAsync(inputPath, outputDirectory, progress, ct);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <inheritdoc />
    public string EncryptedFileExtension => SymmetricEncryption.EncryptedFileExtension;
}
