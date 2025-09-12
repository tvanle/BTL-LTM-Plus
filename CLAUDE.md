# WordBrain Multiplayer Project

## Overview
Chuyển đổi game WordBrain từ single-player thành multiplayer realtime theo phong cách Quizizz/Kahoot với:
- Client-Server architecture sử dụng TCP Socket
- Realtime gameplay với sync timing
- Room-based multiplayer với RoomCode
- Live leaderboard và scoring system
- Booster/Power-ups system

## Architecture

### Client (Unity)
- **Networking**: TCP Socket client để connect server
- **UI**: Room management, lobby, gameplay, leaderboard
- **Game Logic**: Local validation + server verification
- **Real-time Updates**: Receive game state từ server

### Server (.NET Core)
- **Protocol**: TCP Socket server với custom protocol
- **Architecture**: Clean Architecture + SOLID principles
- **Database**: Entity Framework Core (SQL Server/PostgreSQL)
- **Features**: Room management, user authentication, game logic, leaderboard

## Project Structure

```
WordBrain/
├── Client/ (Unity Project hiện tại)
├── Server/ (sẽ tạo .NET Core project)
│   ├── WordBrain.Domain/          # Entities, Value Objects
│   ├── WordBrain.Application/     # Use Cases, Services
│   ├── WordBrain.Infrastructure/  # Database, External services
│   ├── WordBrain.Network/         # TCP Server, Protocol handling
│   └── WordBrain.API/            # Entry point, DI container
└── Shared/ (Protocol definitions, DTOs)
```

## Implementation Plan

### Phase 1: Server Foundation
1. Tạo .NET Core server project với Clean Architecture
2. Implement TCP Socket server
3. Define network protocol (messages, serialization)
4. Basic room management
5. User authentication & database

### Phase 2: Core Multiplayer Features
1. Room creation/joining system
2. Player synchronization trong lobby
3. Game state management
4. Real-time messaging system
5. Basic scoring system

### Phase 3: Advanced Features
1. Live leaderboard updates
2. Booster/Power-ups system
3. Streak & tie-break logic
4. Performance optimization
5. Anti-cheat measures

### Phase 4: Client Integration
1. Refactor Unity client cho multiplayer
2. Network manager integration
3. UI updates cho multiplayer
4. Testing & debugging
5. Deploy & monitoring

## Technical Requirements

- **Server**: .NET Core 6+, Entity Framework Core, TCP Sockets
- **Client**: Unity 2022.3+, async/await patterns
- **Database**: SQL Server hoặc PostgreSQL
- **Protocol**: Custom binary protocol over TCP
- **Authentication**: JWT tokens
- **Real-time**: Event-driven architecture

## Commands for Development

### Build Server
```bash
cd Server
dotnet build
dotnet run --project WordBrain.API
```

### Test
```bash
dotnet test
```

### Database Migration
```bash
dotnet ef migrations add InitialCreate --project WordBrain.Infrastructure
dotnet ef database update --project WordBrain.Infrastructure
```

## Development Notes

- Sử dụng SOLID principles throughout
- Implement Repository pattern cho database access
- Event-driven architecture cho real-time features
- Proper error handling và logging
- Unit testing cho business logic
- Integration testing cho network features