# Telegram Bot for Voice Message Exchange

## Overview

This is a C# Telegram bot designed to receive and store voice messages from users, then randomly send back another user's voice message. The goal is to simulate "Message in a Bottle", using telegram as a delivery medium.

## Dependencies

- `Telegram.Bot`
- `Microsoft.Data.Sqlite`

## Key Features

### Logging User Messages and Metadata

The bot captures the metadata and voice messages from the user and logs them into SQLite database tables.

### Voice Message Handling

The bot listens for incoming voice messages, validates their length, and saves the voice messages as `.ogg` files.

### File Hashing

For each received voice message, the bot calculates a hash value to identify unique messages. Bot will prevent sending the same message over and over again (by checking MD5) to avoid spam.

### Commands

- `/start`: Welcomes the user and logs their information.
- `/last`: Sends the most recent voice message.
- `/random`: Sends a random voice message from the database.

### Exception Handling

Error conditions such as hash calculation failures, and empty databases are handled gracefully with appropriate text messages.

### Other Message Types

The bot also responds to other types of messages, but essentially requests voice messages. It responds to stickers, audio, and video messages but does not process them.

## Code Segments

### SQLite Connection and Query

Uses Microsoft's `SqliteCommand` for executing SQLite queries for logging and message retrieval.

`string stm = "SELECT filehash FROM Voices ORDER BY filehash DESC LIMIT 100;";`

`using var cmd = new SqliteCommand(stm, conn);`

### File Hashing

Calculates the hash of each file using a utility function.

`string hashvalue = Utilities.calculate_checksum_of_thefile(destinationFilePath);`

### Sending Messages

Uses the `SendVoiceAsync` and `SendTextMessageAsync` methods from the Telegram.Bot library.

`await botClient.SendTextMessageAsync(message.Chat, "Принял ваше голосовое!");`

### Main Method

Initializes the SQLite database and starts the Telegram bot with specified handlers.

```other
bot.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken
);
```
