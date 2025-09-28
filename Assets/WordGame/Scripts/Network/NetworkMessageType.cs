namespace WordGame.Network
{
    public enum NetworkMessageType
    {
        // Client -> Server
        CREATE_ROOM,
        JOIN_ROOM,
        LEAVE_ROOM,
        PLAYER_READY,
        START_GAME,
        LEVEL_COMPLETED,
        LEVEL_TIMEOUT,
        HEARTBEAT,

        // Server -> Client
        ROOM_CREATED,
        ROOM_JOINED,
        PLAYER_JOINED,
        PLAYER_LEFT,
        GAME_STARTED,
        LEVEL_STARTED,  // Start of each level
        LEVEL_ENDED,    // End of each level (show leaderboard)
        GAME_ENDED,     // All levels complete
        ERROR
    }
}