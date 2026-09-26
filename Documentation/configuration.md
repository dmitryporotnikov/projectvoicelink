# Configuration Guide

This guide covers all configuration options, environment variables, retention settings, and persistence options available in **ProjectVoiceLink**.

---

## 1. Overview

ProjectVoiceLink supports dynamic configuration through **Environment Variables** (or a `.env` file when using Docker Compose).

---

## 2. Environment Variables Reference

| Variable | Default Value | Description |
| :--- | :--- | :--- |
| `BOT_TOKEN` | *(Required)* | Telegram Bot API token obtained from [@BotFather](https://t.me/BotFather). |
| `DATABASE_PATH` | `ProjectVoiceLink.db` | SQLite database file path or connection string (e.g. `/app/data/ProjectVoiceLink.db`). |
| `AUDIO_STORAGE_PATH` | `audio_bottles` | Path to directory where `.ogg` voice notes are stored (e.g. `/app/data/audio_bottles`). |
| `MIN_VOICE_LENGTH_SECONDS` | `1` | Minimum allowed duration for incoming voice messages in seconds. |
| `MAX_VOICE_LENGTH_SECONDS` | `180` | Maximum allowed duration for incoming voice messages in seconds (anti-spam). |
| `VOICE_COOLDOWN_SECONDS` | `10` | Cooldown period between voice submissions per user to prevent flood/spam. |
| `COMMAND_COOLDOWN_SECONDS` | `2` | Cooldown period between text commands per user. |
| `RETENTION_DAYS` | `30` | Number of days to retain recordings before automated maintenance purge. |
| `MAINTENANCE_INTERVAL_HOURS` | `24` | Interval in hours between automated background disk maintenance cycles (`0` to disable). |
| `ADMIN_USER_IDS` | *(Empty)* | Comma-separated list of numeric Telegram User IDs authorized for `/stats` and `/purge`. |
| `BANNED_USER_IDS` | *(Empty)* | Comma-separated list of numeric Telegram User IDs blocked from interacting with the bot. |

---

## 3. Configuration Details

### 3.1 Bot Token (`BOT_TOKEN` / `TELEGRAM_BOT_TOKEN`)
The authentication token issued by Telegram's `@BotFather`.
Example:
```bash
BOT_TOKEN="1234567890:ABCdefGhIJKlmNoPQRsTUVwxyZ"
```

### 3.2 Anti-Abuse & Rate-Limiting
- `VOICE_COOLDOWN_SECONDS`: Enforces a delay (default: 10s) before a user can submit another voice note.
- `COMMAND_COOLDOWN_SECONDS`: Prevents spamming bot commands (default: 2s).
- `MAX_VOICE_LENGTH_SECONDS`: Protects disk space by rejecting excessively long files (default: 180s = 3 minutes).

### 3.3 Automated Maintenance & Retention
- `RETENTION_DAYS`: Recordings older than this value will have their physical files and database rows removed automatically (default: 30 days).
- `MAINTENANCE_INTERVAL_HOURS`: Background job frequency (default: 24 hours).

### 3.4 Admin Access (`ADMIN_USER_IDS`)
Provide numeric Telegram user IDs separated by commas:
```bash
ADMIN_USER_IDS="12345678,87654321"
```
Users listed here can run:
- `/stats` — displays server disk usage, active voices, user count, and uptime.
- `/purge [days]` — triggers an immediate on-demand disk purge.

---

## 4. Docker Compose `.env` Example

Create a `.env` file in the repository root:

```env
BOT_TOKEN=1234567890:ABCdefGhIJKlmNoPQRsTUVwxyZ
DATABASE_PATH=/app/data/ProjectVoiceLink.db
AUDIO_STORAGE_PATH=/app/data/audio_bottles
MIN_VOICE_LENGTH_SECONDS=1
MAX_VOICE_LENGTH_SECONDS=180
VOICE_COOLDOWN_SECONDS=10
COMMAND_COOLDOWN_SECONDS=2
RETENTION_DAYS=30
MAINTENANCE_INTERVAL_HOURS=24
ADMIN_USER_IDS=12345678
BANNED_USER_IDS=
```
