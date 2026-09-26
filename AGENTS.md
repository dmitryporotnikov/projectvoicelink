# AI Coding Agent Guidelines for ProjectVoiceLink

This document contains instructions, architectural context, conventions, and operational rules for AI coding agents interacting with the **ProjectVoiceLink** repository.

---

## 1. Project Overview & Context

- **Repository**: `projectvoicelink`
- **Application Type**: High-concurrency Telegram Bot Console Application ("Message in a Bottle" anonymous voice exchange).
- **Core Technology Stack**:
  - **Language**: C# 12 / 13
  - **Target Framework**: `.NET 10.0` (LTS)
  - **Dependencies**:
    - `Telegram.Bot` (v19+ / v22+)
    - `Microsoft.Data.Sqlite` (v10.0+)
    - `System.Text.Json` (Native .NET JSON serialization)
- **Data Persistence**:
  - Embedded SQLite database (`ProjectVoiceLink.db`) configured in **WAL mode** with connection pooling.
  - Persistent host-visible file storage directory (`audio_bottles/`) storing `.ogg` voice files.

---

## 2. Repository Layout

```
projectvoicelink/
  |-- Documentation/               # Comprehensive technical docs
  |   |-- README.md                # Documentation index
  |   |-- architecture.md          # Architecture & SQLite schemas
  |   |-- configuration.md         # Configuration & environment variables
  |   |-- handlers_and_features.md # Bot features, localization & spam defense
  |   |-- deployment.md            # Docker, host filesystem visibility & maintenance
  |   |-- development.md           # Developer guidelines & build steps
  |   |-- AGENTS.md                # Detailed agent guide
  |-- ProjectVoiceLink/            # Main .NET 10 application
  |   |-- Configuration.cs         # Environment-driven configuration
  |   |-- DatabaseService.cs       # Concurrency, pooling, WAL mode & queries
  |   |-- Localization.cs          # Multi-language localization engine
  |   |-- MaintenanceService.cs    # Disk cleaner, purge engine, orphan scanner
  |   |-- Program.cs               # App lifecycle, update dispatcher
  |   |-- SpamProtectionService.cs # Sliding window rate limiter & duration bounds
  |   |-- UserClass.cs             # Telegram user & message data model
  |   |-- Utilities.cs             # Fast MD5 hashing & byte formatting
  |   |-- ProjectVoiceLink.csproj  # Main project definition
  |-- ProjectVoiceLink.Tests/      # Unit tests (xUnit)
  |-- Dockerfile                   # Multi-stage container build (.NET 10)
  |-- docker-compose.yml           # Compose orchestration with host bind mount
  |-- AGENTS.md                    # Root AI agent guide
  |-- README.md                    # Root repository overview
```

---

## 3. Core Architectural Rules & Conventions

When modifying or extending this codebase, AI agents must adhere to the following rules:

### 3.1 Concurrency & Database Best Practices
- **Connection Pooling**: Never use a single shared `SqliteConnection` instance across threads. Always acquire a short-lived connection via `databaseService.CreateConnection()`.
- **WAL Mode**: Keep SQLite Write-Ahead Logging active (`PRAGMA journal_mode = WAL;`) to allow concurrent reads and writes without lock contention.
- **Parameterized Queries**: ALWAYS use parameterized SQL queries (`cmd.Parameters.AddWithValue(...)` or typed parameters). NEVER interpolate or concatenate user strings into SQL queries.
- **Indexes**: Maintain indexes on `filehash`, `id`, and `date` to ensure $O(1)$ duplicate checks and fast retention purges.

### 3.2 Server File System Visibility & Disk Maintenance
- Audio files must be stored with transparent, identifiable filenames (`{timestamp}_{userId}_{fileId}.ogg`).
- Docker Compose must use a direct bind mount (`./data:/app/data`) so host administrators can inspect files.
- Storage maintenance must be supported both via background timer (`PeriodicTimer`) and admin commands (`/purge`, `/stats`).

### 3.3 Spam & Abuse Protection
- Enforce sliding-window rate limits for voice submissions (`VOICE_COOLDOWN_SECONDS`) and commands (`COMMAND_COOLDOWN_SECONDS`).
- Reject audio recordings outside of minimum/maximum bounds (`MIN_VOICE_LENGTH_SECONDS`, `MAX_VOICE_LENGTH_SECONDS`).
- Deduplicate voice messages via MD5 checksum and delete duplicate uploads immediately.

### 3.4 Localization
- Do not attempt IP lookups for localization; Telegram does not expose user IP addresses.
- Use `message.From.LanguageCode` and allow manual override via `/lang` command.
- Store user language preference in the `Users` table and retrieve all user-facing strings through `Localization.Get(...)`.

---

## 4. Build, Test, and Verification Commands

### Build Solution
```bash
dotnet build
```

### Run Automated Tests
```bash
dotnet test
```

### Run Application Locally
```bash
dotnet run --project ProjectVoiceLink
```

---

## 5. Definition of Done for Changes
Before concluding any task:
1. `dotnet build` must compile cleanly with 0 errors and 0 warnings.
2. `dotnet test` must execute all unit tests successfully.
3. No secrets or tokens must be committed to git.
4. Documentation in `Documentation/` must be kept up to date.
