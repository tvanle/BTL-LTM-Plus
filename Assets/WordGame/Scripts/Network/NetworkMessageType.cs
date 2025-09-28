namespace WordGame.Network
{
    public enum NetworkMessageType
    {
        ROOM_CREATED,
        ROOM_JOINED,
        PLAYER_JOINED,
        PLAYER_LEFT,
        PLAYER_READY,
        GAME_STARTED,
        NEXT_LEVEL,
        ANSWER_RESULT,
        GAME_ENDED
    }
}