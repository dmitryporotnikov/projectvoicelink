# Bot Handlers, Localization & Features

This document provides a detailed overview of Telegram update handlers, commands, multi-language localization, anti-spam mechanisms, and server maintenance tools in **ProjectVoiceLink**.

---

## 1. Telegram SDK & User Data Exposure

A common question when developing Telegram bots is whether user IP addresses can be detected (e.g. for GeoIP resolution):

### Can Telegram Bots see the user's IP address?
**No.** The Telegram architecture operates via cloud relays:
- All Telegram client traffic connects directly to Telegram's data centers.
- Telegram servers forward messages to your bot application over HTTPS (via long polling or webhooks).
- The bot's TCP connection is strictly with Telegram's reverse-proxy servers.
- **Telegram never exposes the user's IP address** to bots in standard chats (the only exception is if a user launches a Telegram Mini App/Web App on an external web server).

### How ProjectVoiceLink Solves Localization
Instead of relying on unavailable IP lookups:
1. **Automatic Detection**: Every Telegram update includes `message.From.LanguageCode` (e.g. `en`, `ru`, `es`, `de`, `uk`). This reflects the language setting of the user's Telegram application.
2. **Explicit User Choice (`/lang`)**: Users can change their language preference at any time. The bot saves this choice in the SQLite `Users` table and respects it across all future interactions.
3. **Graceful Fallback**: If a language is unrecognized, the bot defaults to Russian or English.

Supported languages:
- 🇷🇺 **Russian (`ru`)**
- 🇬🇧 **English (`en`)**
- 🇪🇸 **Spanish (`es`)**
- 🇩🇪 **German (`de`)**
- 🇺🇦 **Ukrainian (`uk`)**
- 🇵🇹 **Portuguese (`pt`)**
- 🇵🇱 **Polish (`pl`)**

---

## 2. Supported Commands

### User Commands

| Command | Description |
| :--- | :--- |
| `/start` | Welcomes the user with a localized greeting and logs the user interaction. |
| `/last` | Plays back the most recent voice message from the database. |
| `/random` | Plays back a random voice message from another user. |
| `/lang` | Shows available language options. |
| `/lang <code>` | Sets language preference (e.g. `/lang en`, `/lang ru`, `/lang es`, `/lang de`, `/lang uk`, `/lang pt`, `/lang pl`). |

### Admin Commands (Restricted to `ADMIN_USER_IDS`)

| Command | Description |
| :--- | :--- |
| `/stats` | Displays bot uptime, total voices, registered users, log count, SQLite DB file size, and total disk space used by audio files. |
| `/purge` | Triggers an immediate maintenance purge of recordings older than `RETENTION_DAYS` (default 30 days). |
| `/purge <days>` | Triggers an immediate purge for records older than the specified number of days (e.g. `/purge 7`). |

---

## 3. Anti-Spam & Abuse Protection

ProjectVoiceLink implements multi-layered spam defense:

### 3.1 Rate Limiting (Sliding Window Cooldowns)
- **Voice Cooldown** (`VOICE_COOLDOWN_SECONDS`, default: 10s): Users attempting to spam voice messages in rapid succession receive a polite cooldown notification with the remaining wait time.
- **Command Cooldown** (`COMMAND_COOLDOWN_SECONDS`, default: 2s): Protects against flooding text commands.

### 3.2 Duplicate Detection (Global MD5 Checksum)
- When a voice file is downloaded, its MD5 checksum is calculated asynchronously using hardware acceleration (`MD5.HashDataAsync`).
- The database is queried via indexed lookup (`idx_voices_filehash`).
- If the hash already exists:
  - The downloaded file is immediately deleted from disk to prevent storage waste.
  - The user is warned that duplicate recordings cannot be shared.

### 3.3 Duration Boundaries
- **Minimum Duration** (`MIN_VOICE_LENGTH_SECONDS`, default: 1s): Rejects empty or accidental taps.
- **Maximum Duration** (`MAX_VOICE_LENGTH_SECONDS`, default: 180s): Prevents users from uploading long podcasts or files that would exhaust server storage.

### 3.4 User Banlist
- Users specified in `BANNED_USER_IDS` are silently dropped or warned.

---

## 4. Voice Processing & Random Dispatch Pipeline

```
1. Receive Voice Note
   |
2. Validate Duration & Check Rate Limit
   |
3. Stream Download to ./data/audio_bottles/{timestamp}_{userId}_{fileId}.ogg
   |
4. Asynchronous MD5 Checksum Calculation
   |
5. Duplicate Hash Check in SQLite (O(1) Indexed)
   |-- Duplicate? -> Delete downloaded file -> Warn user -> Stop
   |
6. Save Record in `Voices` Table
   |
7. Select Random Voice:
   |-- Query Voices WHERE filehash != currentHash AND id != userId
   |-- Verify file exists on disk
   `-- Stream audio note back to user
```

---

## 5. Non-Voice Media Filtering

When users send non-voice attachments, the bot responds in their preferred language with clear guidance:
- **Stickers**: *"🎨 Стикеры не помещаются в бутылку! Пожалуйста, запишите голосовое сообщение."*
- **Audio Files**: *"🎵 Пожалуйста, отправьте именно голосовое сообщение, записанное в микрофон..."*
- **Video / Notes / Location**: *"📦 Бот принимает только голосовые сообщения..."*
- **Text Messages**: Guided to record a voice message.
