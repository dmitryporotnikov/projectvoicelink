# ProjectVoiceLink Documentation

Welcome to the technical documentation for **ProjectVoiceLink** (Telegram Voice Message Exchange Bot).

## Table of Contents

- [Overview & Purpose](#overview--purpose)
- [Documentation Index](#documentation-index)
- [Quick Start](#quick-start)
- [Architecture Summary](#architecture-summary)
- [Key Features](#key-features)

---

## Overview & Purpose

**ProjectVoiceLink** is an asynchronous Telegram bot developed in C# on **.NET 10 LTS** that enables an anonymous voice message exchange inspired by the concept of a *"Message in a Bottle"*.

When a user drops a voice note into the bot:
1. Duration and rate-limiting cooldowns are checked to prevent flood and abuse.
2. The voice file is streamed to the server disk with human-readable timestamps and user IDs.
3. A hardware-accelerated MD5 hash is computed to guarantee uniqueness.
4. If unique, the message is stored in SQLite and a random voice note from another user is sent back.
5. Storage files are directly visible on the host server file system, and automated maintenance regularly purges expired recordings.

---

## Documentation Index

| Document | Description |
| :--- | :--- |
| [Architecture & Database Design](architecture.md) | High-concurrency architecture, SQLite WAL mode, database schemas, and data pipelines. |
| [Configuration Guide](configuration.md) | Bot token, storage paths, retention policies, and environment variables. |
| [Bot Handlers, Localization & Features](handlers_and_features.md) | Telegram update handlers, multi-language localization, anti-spam mechanisms, and commands. |
| [Deployment & Server Administration](deployment.md) | Docker Compose host volume visibility, VPS disk management, maintenance tasks, and CLI inspection. |
| [Development & Testing Guide](development.md) | Build instructions, xUnit test suite, coding conventions, and concurrency best practices. |
| [AI Coding Agent Guidelines](AGENTS.md) | Rules and instructions for AI coding agents. |

---

## Quick Start

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Telegram Bot Token from [@BotFather](https://t.me/BotFather)

### Running Locally

```bash
export BOT_TOKEN="your-telegram-bot-token"
dotnet restore
dotnet build
dotnet test
dotnet run --project ProjectVoiceLink
```

### Running with Docker Compose (Host-Visible Storage)

```bash
export BOT_TOKEN="your-telegram-bot-token"
docker compose up -d --build
```
All database and audio files will appear directly in `./data/` on your host machine.
