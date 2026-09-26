# ProjectVoiceLink - Telegram Voice Message Exchange

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0%20LTS-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](Dockerfile)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

An asynchronous, high-concurrency Telegram Bot built with C# and **.NET 10 LTS** designed to exchange voice messages randomly between users ("Message in a Bottle" concept).

---

## Quick Navigation

- [Documentation Index](Documentation/README.md)
- [Architecture & Database Design](Documentation/architecture.md)
- [Configuration & Environment Variables](Documentation/configuration.md)
- [Handlers, Localization & Features](Documentation/handlers_and_features.md)
- [Deployment & Server Administration](Documentation/deployment.md)
- [Development & Testing Guide](Documentation/development.md)
- [AI Coding Agent Guidelines](AGENTS.md)

---

## Key Features

- **Anonymous Voice Exchange**: Users drop a voice note into the bot and receive a random voice note from another user in return.
- **High Concurrency & Performance**: SQLite Write-Ahead Logging (WAL mode), connection pooling, hardware-accelerated MD5 hashing (`MD5.HashDataAsync`), and $O(1)$ indexed lookups.
- **Server File System Visibility**: Audio files and SQLite database are directly stored on the host file system (`./data/`) with human-readable timestamps and user IDs.
- **Automated Disk Maintenance**: Scheduled background cleaner (`PeriodicTimer`) and admin commands (`/purge`, `/stats`) that automatically delete expired voice files, purge database records, clean orphan files, and compact SQLite.
- **Multi-Layered Spam & Abuse Protection**:
  - Sliding-window rate limiting on voice submissions (`VOICE_COOLDOWN_SECONDS`) and commands (`COMMAND_COOLDOWN_SECONDS`).
  - Global duplicate detection by MD5 hash (duplicate files are immediately deleted).
  - Configurable recording duration boundaries (`MIN_VOICE_LENGTH_SECONDS` and `MAX_VOICE_LENGTH_SECONDS`).
  - User banlist support (`BANNED_USER_IDS`).
- **Multi-Language Localization**:
  - Automatic detection via Telegram client `LanguageCode` (`ru`, `en`, `es`, `de`, `uk`, `pt`, `pl`).
  - Explicit language switching via `/lang` command.
  - Explanation of Telegram's architecture (Telegram does not expose user IP addresses; client language tags are used instead).

---

## Quick Start

### 1. Run with Docker Compose (Host-Visible Storage)

```bash
# Set your Telegram Bot Token
export BOT_TOKEN="your_bot_token_from_botfather"

# Start container (files will be stored in ./data on your host)
docker compose up -d --build
```

### 2. Run Locally with .NET CLI

```bash
export BOT_TOKEN="your_bot_token_from_botfather"

dotnet restore
dotnet build
dotnet test
dotnet run --project ProjectVoiceLink
```

---

## Commands

### User Commands
- `/start`: Registers the user and sends a localized welcome greeting.
- `/last`: Plays back the most recent voice message.
- `/random`: Plays back a random voice message from another user.
- `/lang [code]`: Switch language (e.g. `/lang en`, `/lang ru`, `/lang es`, `/lang de`, `/lang uk`, `/lang pt`, `/lang pl`).

### Admin Commands (Requires `ADMIN_USER_IDS`)
- `/stats`: Displays server disk usage, active voice count, registered users, and uptime.
- `/purge [days]`: Purges voice files and database records older than the specified days.

---

## Technology Stack

- **Framework**: .NET 10.0 LTS
- **Language**: C# 12 / 13
- **Telegram Client**: `Telegram.Bot` 19.0+
- **Database**: `Microsoft.Data.Sqlite` 10.0+ (WAL mode, connection pooling)
- **Test Framework**: `xUnit` 2.9+
