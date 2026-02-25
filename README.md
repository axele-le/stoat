<p align="center">
  <img src="src/Stoat/Assets/Images/stoat-white.svg" alt="Stoat Logo" width="120" />
</p>

<h1 align="center">Stoat</h1>

<p align="center">
  <strong>Offline cryptographic toolkit with multi-profile support</strong>
</p>

<p align="center">
  <a href="https://github.com/axele-le/stoat/blob/main/LICENSE.md"><img src="https://img.shields.io/github/license/axele-le/stoat?style=flat-square&color=blue" alt="License" /></a>
  <img src="https://img.shields.io/github/repo-size/axele-le/stoat?style=flat-square&color=green" alt="Repo Size" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Avalonia-11.3-8b44ac?style=flat-square" alt="Avalonia" />
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-0078D6?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/version-0.8.0-orange?style=flat-square" alt="Version" />
</p>

---

Stoat is a desktop application that puts a full set of cryptographic tools at your fingertips — **entirely offline**. No cloud, no accounts, no data leaving your machine.

It is designed around **profiles**: isolated environments where each set of keys, certificates, and credentials lives independently. Switch between personal, work, and project contexts without overlap.

---

## Features

| Feature | Description |
|---|---|
| **Encryption & Decryption** | Encrypt text and files with 13+ symmetric ciphers (AES, ChaCha20, Twofish, Serpent...), multiple block modes, and configurable key derivation (PBKDF2, Argon2id, Scrypt). Save your favourite configurations as presets. |
| **PEM Key Management** | Generate, import, and export RSA key pairs (2048–4096+ bit). Private keys are encrypted at rest with DPAPI. |
| **Certificate Signing Requests** | Create CSRs with full subject DN, SAN, key usage extensions, and multiple signature algorithms (RSA-PKCS#1, RSA-PSS, ECDSA). Import and inspect existing CSRs. |
| **API Key Generation** | Generate cryptographically secure API keys with custom length, complexity, and prefix. |
| **Hashing** | Hash text and files with 16+ algorithms (SHA-2, SHA-3, BLAKE2b, Whirlpool...) and verify against known hashes. |
| **Master Password & Lock** | Protect the app with a master password and automatic lock on inactivity. Recovery key included. |
| **Profile Export & Import** | Export profiles to an encrypted `.stoat` package (AES-256-GCM, 200k PBKDF2 iterations) and import them on another machine. |

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Build & Run

```bash
# Clone the repository
git clone https://github.com/axele-le/stoat.git
cd stoat

# Build
dotnet build

# Run
dotnet run --project src/Stoat/Stoat.csproj
```

### Development (hot reload)

```bash
dotnet watch run --project src/Stoat/Stoat.csproj
```

## Project Structure

```
stoat/
├── src/
│   ├── Stoat/              # UI layer (Avalonia views, styles, assets)
│   ├── Stoat.Core/         # ViewModels (MVVM with CommunityToolkit)
│   └── Stoat.Services/     # Business logic, cryptography, storage
└── Stoat.sln
```

## Branches

* `master` - default branch representing the current state.

## Localization

| | Language |
|---|---|
| 🇬🇧 | English |
| 🇮🇹 | Italiano |
| 🇫🇷 | Français |
| 🇩🇪 | Deutsch |
| 🇪🇸 | Español |
| 🇵🇱 | Polski |

## Roadmap

Stoat is currently in **pre-release**.

All core features are implemented and functional, but the project has not yet reached a stable 1.0 release.

The current focus is on:
- Reviewing and improving UI/UX responsiveness across different screen sizes and resolutions
- Identifying areas where the user experience can be refined

A v1.0 release will follow once these checks are completed and no major issues remain.

## License

This project is licensed under the [Apache License 2.0](LICENSE.md).
