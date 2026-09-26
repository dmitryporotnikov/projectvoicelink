# Deployment & Server Administration Guide

This guide details how to build, deploy, manage, and inspect **ProjectVoiceLink** on a server/VPS using Docker Compose or native hosting.

---

## 1. Server File System Visibility

A key architectural design in ProjectVoiceLink is that **all data files (database, WAL journals, and voice files) are stored transparently on the host server's file system**.

### Directory Structure on Host

When running with Docker Compose, the `./data` directory is mounted directly to the host:

```
projectvoicelink/
  |-- data/                               # Visible directly on the host VPS!
  |   |-- ProjectVoiceLink.db             # Main SQLite database
  |   |-- ProjectVoiceLink.db-wal         # Write-Ahead Log journal
  |   |-- ProjectVoiceLink.db-shm         # Shared-memory index
  |   |-- audio_bottles/                  # Voice recordings directory
  |        |-- 20260926_143000_123456_fileA.ogg
  |        |-- 20260926_143115_789012_fileB.ogg
  |-- docker-compose.yml
  |-- Dockerfile
```

### Inspecting Files Directly on Host
As a server administrator, you can list, check disk space, and inspect files at any time without entering the container:

```bash
# List all saved audio bottles ordered by time
ls -lht ./data/audio_bottles/

# Check total disk space consumed by audio bottles
du -sh ./data/audio_bottles/

# Inspect SQLite database directly using sqlite3 CLI
sqlite3 ./data/ProjectVoiceLink.db "SELECT count(1) FROM Voices;"
```

---

## 2. Docker Deployment (Recommended)

### 2.1 Configuration
Create a `.env` file in the project root:

```env
BOT_TOKEN=1234567890:ABCdefGhIJKlmNoPQRsTUVwxyZ
ADMIN_USER_IDS=123456789
RETENTION_DAYS=30
```

### 2.2 Docker Compose (`docker-compose.yml`)
The repository includes a ready-to-use Compose configuration with a direct host volume bind-mount:

```yaml
services:
  projectvoicelink:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: projectvoicelink
    restart: unless-stopped
    environment:
      - BOT_TOKEN=${BOT_TOKEN:-YOURBOTTOKEN}
      - DATABASE_PATH=/app/data/ProjectVoiceLink.db
      - AUDIO_STORAGE_PATH=/app/data/audio_bottles
      - MIN_VOICE_LENGTH_SECONDS=1
      - MAX_VOICE_LENGTH_SECONDS=180
      - VOICE_COOLDOWN_SECONDS=10
      - COMMAND_COOLDOWN_SECONDS=2
      - RETENTION_DAYS=30
      - MAINTENANCE_INTERVAL_HOURS=24
      - ADMIN_USER_IDS=${ADMIN_USER_IDS:-}
      - BANNED_USER_IDS=${BANNED_USER_IDS:-}
    volumes:
      - ./data:/app/data
```

### 2.3 Management Commands

```bash
# Start bot in background
docker compose up -d --build

# View live logs
docker compose logs -f

# Check container status
docker compose ps

# Stop bot gracefully (SIGINT/SIGTERM will trigger database flush and close)
docker compose down
```

---

## 3. Storage Maintenance & Housekeeping

### Automated Housekeeping
ProjectVoiceLink runs a background task every 24 hours (`MAINTENANCE_INTERVAL_HOURS=24`):
1. Deletes `.ogg` audio files older than `RETENTION_DAYS`.
2. Deletes expired database rows from `Voices` and `Logs`.
3. Scans for and removes any orphaned audio files on disk that lack matching database entries.
4. Executes `VACUUM` to compact SQLite file size.

### On-Demand Telegram Admin Purge
Bot administrators (configured in `ADMIN_USER_IDS`) can execute maintenance directly via Telegram:
- `/stats` — inspect real-time disk and database stats.
- `/purge` — triggers purge with configured retention limit.
- `/purge 7` — purges everything older than 7 days immediately.

### Manual Server-Side Cleanup
If you need to manually purge audio files from the server terminal:

```bash
# Find and delete audio files older than 30 days
find ./data/audio_bottles/ -name "*.ogg" -type f -mtime +30 -delete

# Run maintenance vacuum from host
sqlite3 ./data/ProjectVoiceLink.db "VACUUM;"
```
*(The bot handles missing files gracefully during playback by checking `File.Exists` before sending).*
