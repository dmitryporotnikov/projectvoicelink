# Development & Testing Guide

This guide describes how to set up your local development environment, build the solution, run automated tests, and contribute changes to **ProjectVoiceLink**.

---

## 1. Development Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or latest installed SDK)
- Visual Studio 2022 / 2025, JetBrains Rider, or VS Code with C# Dev Kit.
- Git.

---

## 2. Solution Structure

```
projectvoicelink/
  |-- Documentation/               # Technical documentation
  |-- ProjectVoiceLink/            # Core .NET 10 application
  |   |-- Configuration.cs         # Environment-driven configuration
  |   |-- DatabaseService.cs       # SQLite connection pooling & WAL queries
  |   |-- Localization.cs          # Multi-language string engine
  |   |-- MaintenanceService.cs    # Disk cleanup, orphan scanner, purge engine
  |   |-- Program.cs               # Telegram update dispatcher & bot lifecycle
  |   |-- SpamProtectionService.cs # Sliding window rate-limiter & duration check
  |   |-- UserClass.cs             # Telegram user & message data model
  |   |-- Utilities.cs             # Hardware-accelerated MD5 & formatting
  |   |-- ProjectVoiceLink.csproj  # .NET 10 project definition
  |-- ProjectVoiceLink.Tests/      # Automated xUnit test suite (27 tests)
  |   |-- DatabaseTests.cs         # Concurrency, WAL mode & CRUD tests
  |   |-- LocalizationTests.cs     # Language resolution & translation tests
  |   |-- MaintenanceTests.cs      # Purge & storage metrics tests
  |   |-- SpamProtectionTests.cs   # Cooldown & duration tests
  |   |-- UtilitiesTests.cs        # MD5 checksum tests
  |-- Dockerfile                   # Multi-stage production container build
  |-- docker-compose.yml           # Host-visible volume orchestration
  |-- AGENTS.md                    # AI coding agent guide
  |-- README.md                    # Project overview
```

---

## 3. Building and Running Locally

### 3.1 Restore and Build
```bash
dotnet restore
dotnet build
```

### 3.2 Running Unit Tests
```bash
dotnet test
```

### 3.3 Running Locally
```bash
# Set your bot token
export BOT_TOKEN="your_telegram_bot_token"
dotnet run --project ProjectVoiceLink
```

---

## 4. Coding Conventions & Best Practices

1. **Target Runtime**: Always target `.NET 10.0` (or latest LTS).
2. **Database Concurrency**:
   - Always acquire short-lived connections via `databaseService.CreateConnection()`.
   - Never share a single `SqliteConnection` instance across concurrent threads.
   - Always maintain `PRAGMA journal_mode = WAL;` and use parameterized queries.
3. **Localization**:
   - Never hardcode user-facing strings in `Program.cs`. Always add them to `Localization.cs` across all supported languages (Russian, English, Spanish, German, Ukrainian, Portuguese, Polish).
4. **Spam Defense**:
   - Ensure all incoming voice notes are checked for rate limits, minimum/maximum duration, and deduplication.
5. **Deterministic Cleanup**:
   - All `FileStream` and `SqliteDataReader` objects must be wrapped in `using` or `await using` blocks.
