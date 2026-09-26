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

> 📖 **Production Deployment Guide**: For full instructions on hosting with Docker, host filesystem visibility (`./data`), volume backups, and VPS maintenance, check the [Deployment & Server Administration Guide](Documentation/deployment.md).

### 2. Run Locally with .NET CLI

```bash
export BOT_TOKEN="your_bot_token_from_botfather"

dotnet restore
dotnet build
dotnet test
dotnet run --project ProjectVoiceLink
```

---

## Supported Environment Variables (`.env`)

You can configure the application via a `.env` file or system environment variables:

| Variable | Default | Description |
| :--- | :---: | :--- |
| `BOT_TOKEN` | *(Required)* | Telegram Bot API token from [@BotFather](https://t.me/BotFather). |
| `DATABASE_PATH` | `/app/data/ProjectVoiceLink.db` | SQLite database file path or connection string. |
| `AUDIO_STORAGE_PATH` | `/app/data/audio_bottles` | Host/container directory path for storing `.ogg` voice files. |
| `MIN_VOICE_LENGTH_SECONDS` | `1` | Minimum recording duration accepted by the bot. |
| `MAX_VOICE_LENGTH_SECONDS` | `180` | Maximum recording duration accepted by the bot. |
| `VOICE_COOLDOWN_SECONDS` | `3` | Minimum cooldown between consecutive voice messages (`0` to disable). |
| `VOICE_WINDOW_SECONDS` | `30` | Duration of the sliding throttle window for voice messages. |
| `VOICE_MAX_PER_WINDOW` | `3` | Maximum voice messages allowed within the sliding window. |
| `COMMAND_COOLDOWN_SECONDS` | `1` | Minimum cooldown between consecutive text commands (`0` to disable). |
| `COMMAND_WINDOW_SECONDS` | `10` | Duration of the sliding throttle window for commands. |
| `COMMAND_MAX_PER_WINDOW` | `5` | Maximum commands allowed within the sliding window. |
| `RETENTION_DAYS` | `30` | Number of days to retain recordings before automated maintenance purge. |
| `MAINTENANCE_INTERVAL_HOURS` | `24` | Interval in hours between automated background maintenance cycles (`0` to disable). |
| `ADMIN_USER_IDS` | *(Empty)* | Comma-separated list of numeric Telegram User IDs authorized for `/stats` and `/purge`. |
| `BANNED_USER_IDS` | *(Empty)* | Comma-separated list of numeric Telegram User IDs blocked from interacting with the bot. |

For detailed configuration instructions and examples, see the [Configuration Guide](Documentation/configuration.md).

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
