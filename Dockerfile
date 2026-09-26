# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["ProjectVoiceLink/ProjectVoiceLink.csproj", "ProjectVoiceLink/"]
RUN dotnet restore "ProjectVoiceLink/ProjectVoiceLink.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/ProjectVoiceLink"
RUN dotnet publish "ProjectVoiceLink.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Install native sqlite3 library for admin tools and SQLite engine
RUN apt-get update && apt-get install -y --no-install-recommends libsqlite3-0 sqlite3 && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Setup persistent data directories
RUN mkdir -p /app/data /app/data/audio_bottles

# Default Environment Variables
ENV DATABASE_PATH="/app/data/ProjectVoiceLink.db"
ENV AUDIO_STORAGE_PATH="/app/data/audio_bottles"
ENV MIN_VOICE_LENGTH_SECONDS="1"
ENV MAX_VOICE_LENGTH_SECONDS="180"
ENV VOICE_COOLDOWN_SECONDS="10"
ENV COMMAND_COOLDOWN_SECONDS="2"
ENV RETENTION_DAYS="30"
ENV MAINTENANCE_INTERVAL_HOURS="24"

# Volume mount point
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "ProjectVoiceLink.dll"]
