# ProjectVoiceLink

[![.NET 10](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](Dockerfile)

A Telegram bot that swaps voice messages between strangers. You send it a voice note, it sends you back someone else's. Message in a bottle, with `.ogg` files.

Built with C# on .NET 10, using `Telegram.Bot` and SQLite in WAL mode. Audio and the database live on the host filesystem under `./data`, so you can look at what's stored without a special tool.

## How it behaves

Voice notes get rate limited with a sliding window, both for submissions and for commands. Duplicates are caught by MD5 hash and deleted, recording length is bounded at both ends, and a banlist takes care of repeat offenders. A background cleaner runs on a timer, deleting expired files and database rows, clearing orphans and compacting SQLite. `/purge` and `/stats` do the same on demand for admins.

The interface speaks Russian, English, Spanish, German, Ukrainian, Portuguese and Polish. Language is picked from the Telegram client's language code on first contact, and `/lang` changes it later.

## Run it

With Docker Compose:

```bash
export BOT_TOKEN="your_bot_token_from_botfather"
docker compose up -d --build
```

Or with the .NET SDK:

```bash
export BOT_TOKEN="your_bot_token_from_botfather"
dotnet restore
dotnet build
dotnet test
dotnet run --project ProjectVoiceLink
```

## Configuration

The important ones, all optional except `BOT_TOKEN`:

| Variable | Default | What it does |
|---|---|---|
| `BOT_TOKEN` | required | Telegram token from [@BotFather](https://t.me/BotFather). |
| `DATABASE_PATH` | `/app/data/ProjectVoiceLink.db` | SQLite file. |
| `AUDIO_STORAGE_PATH` | `/app/data/audio_bottles` | Where voice files go. |
| `MIN_VOICE_LENGTH_SECONDS` | `1` | Shortest accepted recording. |
| `MAX_VOICE_LENGTH_SECONDS` | `180` | Longest accepted recording. |
| `VOICE_COOLDOWN_SECONDS` | `3` | Pause between voice submissions. |
| `RETENTION_DAYS` | `30` | Age at which recordings are purged. |
| `MAINTENANCE_INTERVAL_HOURS` | `24` | Cleaner interval, `0` disables it. |
| `ADMIN_USER_IDS` | empty | Telegram IDs allowed to run `/stats` and `/purge`. |
| `BANNED_USER_IDS` | empty | Telegram IDs blocked from the bot. |

The full list, including the window and per-window limits, is in [Documentation/configuration.md](Documentation/configuration.md).

## Commands

Users get `/start`, `/last`, `/random` and `/lang [code]`. Admins get `/stats`, which reports disk use, voice count, registered users and uptime, and `/purge [days]`, which drops files and rows older than the given number of days.

## Documentation

- [Architecture and database design](Documentation/architecture.md)
- [Configuration](Documentation/configuration.md)
- [Handlers, localization and features](Documentation/handlers_and_features.md)
- [Deployment and server administration](Documentation/deployment.md)
- [Development and testing](Documentation/development.md)

The stack is .NET 10, `Telegram.Bot` 19.0, `Microsoft.Data.Sqlite` 10.0 and xUnit 2.9.

There's no license file in this repo yet, so treat the code as all rights reserved.
