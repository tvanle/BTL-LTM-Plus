# WordBrain Multiplayer Game

## Project Overview
WordBrain multiplayer realtime game với kiến trúc client-server, hỗ trợ nhiều platform (Unity Editor, Windows, Android, iOS).

## Current Architecture

### Client (Unity)
- **Unity 2022.3+** với C# modern features
- **TCP Socket** connection với server
- **JsonUtility** thay thế SimpleJSON
- **async/await** thay thế tất cả IEnumerator/Coroutines
- **LINQ** cho functional programming
- **No third-party**: Đã xóa AdMob, Firebase, GoogleMobileAds

### Server (.NET)
- **.NET 9.0** Console Application
- **Simple TCP Server** - Không dùng layered architecture phức tạp
- **In-Memory State** - ConcurrentDictionary, không cần database
- **Real-time** - Direct socket messaging
- **Single file** - GameServer.cs chứa toàn bộ logic

## Project Structure

```
WordBrain/
├── Assets/
│   └── WordGame/
│       ├── Scripts/
│       │   ├── Network/                    # Multiplayer networking
│       │   │   ├── NetworkManager.cs       # TCP client, handles connection
│       │   │   ├── MultiplayerGameController.cs  # Game UI & logic
│       │   │   └── UnityMainThreadDispatcher.cs  # Thread synchronization
│       │   ├── GamePlay/
│       │   │   ├── GameManager.cs          # Core game logic (refactored)
│       │   │   ├── Timer.cs                # Async timer (no coroutines)
│       │   │   └── WordRegion.cs           # Word grid management
│       │   ├── UI/
│       │   │   ├── HomeManager.cs          # Menu navigation
│       │   │   ├── Dialog.cs               # UI dialogs
│       │   │   └── RewardedButton.cs       # Direct rewards (no ads)
│       │   └── Utilities/
│       │       ├── CUtils.cs               # Common utilities
│       │       └── Utilities.cs            # LINQ-based helpers
│       └── GAMEPLAY.md                     # Game specifications
│
└── Server/
    └── GameServer/
        ├── GameServer.cs                   # Main TCP server (all logic)
        ├── Program.cs                      # Entry point
        └── GameServer.csproj               # .NET 9 project
```

## Key Refactoring Done

### 1. Dependencies Removed
- ✅ AdMob/Unity Ads - All ad code removed
- ✅ Firebase - Analytics & services removed  
- ✅ SimpleJSON - Replaced with JsonUtility
- ✅ GoogleMobileAds - Completely removed

### 2. Code Modernization
- ✅ IEnumerator → async/await Task
- ✅ For loops → LINQ expressions
- ✅ Properties → Fields (for JsonUtility compatibility)
- ✅ Callbacks → Events/Actions pattern

### 3. Multiplayer Implementation
- ✅ TCP Socket server (not REST/HTTP)
- ✅ Room-based system with codes
- ✅ Real-time game synchronization
- ✅ Live scoring & leaderboard

## Running the Project

### Server Setup
```bash
# Navigate to server directory
cd Server/GameServer

# Build the project
dotnet build

# Run server (default port 8080)
dotnet run

# Or run with custom port
dotnet run 9090

# Build standalone exe
dotnet publish -c Release -r win-x64 --self-contained
```

### Client Setup (Unity)

1. **Open Unity Project**
   - Unity 2022.3+ required
   - Open project at root WordBrain folder

2. **Setup Multiplayer Scene**
   ```
   GameObject > Create Empty > "NetworkManager"
   - Add Component: NetworkManager.cs
   - Add Component: MultiplayerGameController.cs
   ```

3. **Configure NetworkManager**
   - Server Host: `localhost` (or server IP)
   - Server Port: `8080`

4. **Configure MultiplayerGameController UI**
   - Create Menu Panel, Room Panel, Game Panel
   - Link all UI elements in Inspector

### Testing Multiplayer

**Local Testing:**
- Run server: `dotnet run`
- Open multiple Unity Editor instances
- Or build multiple .exe files
- All connect to `localhost:8080`

**Network Testing:**
- Server on PC: Get IP with `ipconfig`
- Clients set serverHost to that IP
- Mobile builds: Must use PC's network IP

**Internet Testing:**
```bash
# Use ngrok for public access
ngrok tcp 8080
# Use provided URL in clients
```

## Network Protocol

### Message Flow
```
Client → Server:
├── CREATE_ROOM    {Username, Topic, MaxPlayers}
├── JOIN_ROOM      {RoomCode, Username}
├── LEAVE_ROOM     
├── PLAYER_READY   
├── START_GAME     (Host only)
├── SUBMIT_ANSWER  {Answer, TimeTaken}
└── HEARTBEAT      (Every 10s)

Server → Client:
├── ROOM_CREATED   {RoomCode, PlayerId}
├── ROOM_JOINED    {RoomCode, Players}
├── PLAYER_JOINED  {PlayerId, Username}
├── PLAYER_LEFT    {PlayerId}
├── GAME_STARTED   {Level, Grid, Words}
├── NEXT_LEVEL     {Level, Grid, Words}
├── ANSWER_RESULT  {IsCorrect, Score}
├── GAME_ENDED     {FinalResults}
└── ERROR          {ErrorMessage}
```

### Wire Format
```
[Message Length: 4 bytes][JSON Payload: N bytes]
```

## Game Flow

1. **Lobby Phase**
   - Create room (get 6-digit code)
   - Join with code
   - Wait for players
   - Everyone marks ready

2. **Game Phase**
   - Host starts game
   - Server sends grid + words
   - Players submit answers
   - Real-time score updates
   - Auto-advance levels

3. **End Phase**
   - Show final leaderboard
   - Return to room
   - Can restart

## Development Guidelines

### Code Style
- Use `this` keyword for member access
- Prefer LINQ over loops
- async/await for all async operations
- Minimal comments (self-documenting code)
- Follow existing patterns

### Architecture Principles
- **Simple > Complex** - No over-engineering
- **Game-optimized** - Fast state updates
- **In-memory first** - Database optional
- **Stateless protocol** - Can reconnect

## Common Issues & Solutions

### Can't Connect to Server
```bash
# Check if server is running
netstat -an | findstr 8080

# Open firewall port
netsh advfirewall firewall add rule name="WordBrain" dir=in action=allow protocol=TCP localport=8080
```

### Unity Build Errors
- Ensure .NET Standard 2.1 in Player Settings
- Check JsonUtility serializable classes (public fields)
- Verify async/await compatibility

### Mobile Connection Issues
- Use actual network IP, not localhost
- Same WiFi network required for LAN
- Port forwarding for internet play

## Future Improvements (TODO)

- [ ] Reconnection handling
- [ ] Persistent user profiles
- [ ] Chat system
- [ ] Spectator mode
- [ ] Tournament mode
- [ ] WebGL support (WebSocket adapter)
- [ ] Database integration (optional)
- [ ] Docker deployment

## Quick Commands Reference

```bash
# Server
dotnet run                          # Run server
dotnet build -c Release            # Build optimized
dotnet publish -r win-x64          # Create standalone

# Unity
Build Settings > PC Standalone     # Build Windows
Build Settings > Android           # Build APK
File > Build and Run              # Quick test

# Network Debug
netstat -an | findstr 8080        # Check port
ping [server-ip]                  # Test connection
telnet [server-ip] 8080           # Test TCP
```