# System Architecture & Database Design

This document details the software architecture, component relationships, message processing pipelines, concurrency model, and data storage design for **ProjectVoiceLink**.

---

## 1. High-Level Architecture

ProjectVoiceLink is an asynchronous long-polling Telegram Bot built with C# and **.NET 10 LTS**. It connects to the Telegram Bot API over HTTPS and maintains an embedded, high-performance SQLite database alongside persistent audio storage visible directly on the host file system.

```
                  +-----------------------+
                  |   Telegram Platform   |
                  +-----------+-----------+
                              |
                Update Events | (Long Polling)
                              v
                  +-----------------------+
                  |  TelegramBotClient    |
                  +-----------+-----------+
                              |
                              v
                  +-----------------------+
                  |       Program.cs      |
                  |  (Async Dispatcher)   |
                  +-----+-----+-----+-----+
                        |     |     |     |
            +-----------+     |     |     +-----------+
            |                 |     |                 |
            v                 v     v                 v
   +------------------+  +-------+  +--------+  +--------------------+
   | Spam Protection  |  | Local-|  | Maint- |  | DatabaseService    |
   | - Rate limiter   |  | ization| | enance |  | - Connection pool  |
   | - Duplication    |  | Engine | | Engine |  | - WAL Journal Mode |
   | - Duration check |  +-------+  +--------+  | - Indexed Queries  |
   +------------------+                             +---------+----------+
            |                                                 |
            v                                                 v
  +--------------------+                             +--------------------+
  | Server File System |                             | SQLite Database    |
  | ./data/audio_      |                             | ./data/Project     |
  | bottles/*.ogg      |                             | VoiceLink.db       |
  +--------------------+                             +--------------------+
```

---

## 2. Core Components

### 2.1 `Program.cs`
- **Role**: Application entrypoint, lifecycle orchestration, and update routing.
- **Key Responsibilities**:
  - Initializes services (`DatabaseService`, `SpamProtectionService`, `MaintenanceService`).
  - Registers graceful shutdown hooks (`Console.CancelKeyPress`, `AppDomain.ProcessExit`).
  - Starts background scheduled maintenance tasks.
  - Dispatches updates asynchronously across the threadpool.

### 2.2 `DatabaseService.cs`
- **Role**: High-concurrency database abstraction layer.
- **Key Responsibilities**:
  - Configures SQLite in **WAL (Write-Ahead Logging)** mode (`PRAGMA journal_mode = WAL;`) allowing simultaneous readers and writers.
  - Utilizes connection pooling to prevent multi-threaded lock contentions.
  - Manages schemas and performance indexes (`idx_voices_filehash`, `idx_voices_id`, etc.).
  - Handles parameterized SQL queries and user language preference persistence.

### 2.3 `SpamProtectionService.cs`
- **Role**: In-memory and database-backed abuse protection.
- **Key Responsibilities**:
  - Enforces sliding-window cooldowns per user ID (voice cooldown & command cooldown).
  - Validates recording duration against configurable minimum and maximum limits.
  - Queries indexed filehashes to prevent duplicate recordings globally.
  - Enforces administrative user bans.

### 2.4 `MaintenanceService.cs`
- **Role**: Server storage health and disk maintenance.
- **Key Responsibilities**:
  - Automated background cleanup via `PeriodicTimer`.
  - Removes physical `.ogg` files and database rows older than retention threshold.
  - Scans for and cleans up orphaned files on disk.
  - Executes SQLite `VACUUM` to reclaim disk pages.
  - Calculates real-time disk and database storage statistics for `/stats`.

### 2.5 `Localization.cs`
- **Role**: Multi-lingual response engine.
- **Key Responsibilities**:
  - Resolves user language preference automatically from Telegram client `LanguageCode` (`update.Message.From.LanguageCode`).
  - Allows manual user language overrides via `/lang` command.
  - Provides built-in strings for English (`en`), Russian (`ru`), Spanish (`es`), German (`de`), Ukrainian (`uk`), Portuguese (`pt`), and Polish (`pl`).

### 2.6 `Utilities.cs`
- **Role**: Cryptographic hashing and formatting.
- **Key Responsibilities**:
  - Hardware-accelerated non-blocking MD5 checksum computation via `MD5.HashDataAsync`.
  - Human-readable byte formatting (`FormatBytes`).

---

## 3. High-Concurrency & Performance Architecture

1. **SQLite WAL Mode**:
   Standard SQLite locks the entire database file during writes. In ProjectVoiceLink, `PRAGMA journal_mode = WAL;` and `PRAGMA synchronous = NORMAL;` are activated. Readers never block writers, and writers never block readers.
2. **Connection Pooling**:
   Instead of a single shared connection (which throws concurrent access exceptions in multi-threaded environments), every operation acquires a pooled connection via `CreateConnection()`.
3. **Database Indexing**:
   - `idx_voices_filehash` enables $O(1)$ duplicate lookups.
   - `idx_voices_date` makes retention cleanup instantaneous without table scans.
4. **Asynchronous File Streaming**:
   All audio file reads and writes use asynchronous, buffered `FileStream` instances (`useAsync: true`, 64 KB buffers) with `FileShare.Read`.

---

## 4. Server File System Visibility

All persistent files are stored directly in the `./data` directory on the host (bind-mounted in Docker):

```
data/
  |-- ProjectVoiceLink.db          # Main SQLite database file
  |-- ProjectVoiceLink.db-wal      # WAL journal file (high concurrency)
  |-- ProjectVoiceLink.db-shm      # Shared memory index for WAL
  |-- audio_bottles/               # Voice recordings
       |-- 20260926_143000_123456_AwADBA...ogg
```

### File Naming Convention
Downloaded audio files use the pattern:
```
{timestamp:yyyyMMdd_HHmmss}_{userId}_{fileId}.ogg
```
This enables the server administrator to inspect, grep, or clean files directly from the Linux shell (`ls`, `find`, `du`).

---

## 5. SQLite Database Schema

```sql
CREATE TABLE IF NOT EXISTS Voices (
    KEYID INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT,
    last_name TEXT,
    username TEXT,
    id TEXT,
    language_code TEXT,
    date TEXT,
    is_bot TEXT,
    message_id TEXT,
    audio_file TEXT,
    filehash TEXT
);

CREATE TABLE IF NOT EXISTS Users (
    KEYID INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT,
    last_name TEXT,
    username TEXT,
    id TEXT,
    language_code TEXT,
    preferred_language TEXT,
    date TEXT,
    is_bot TEXT,
    message_id TEXT,
    text TEXT
);

CREATE TABLE IF NOT EXISTS Logs (
    KEYID INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT,
    last_name TEXT,
    username TEXT,
    id TEXT,
    language_code TEXT,
    date TEXT,
    is_bot TEXT,
    message_id TEXT,
    text TEXT
);

-- Performance Indexes
CREATE INDEX IF NOT EXISTS idx_voices_filehash ON Voices(filehash);
CREATE INDEX IF NOT EXISTS idx_voices_id ON Voices(id);
CREATE INDEX IF NOT EXISTS idx_voices_date ON Voices(date);
CREATE INDEX IF NOT EXISTS idx_users_id ON Users(id);
CREATE INDEX IF NOT EXISTS idx_logs_date ON Logs(date);
```
