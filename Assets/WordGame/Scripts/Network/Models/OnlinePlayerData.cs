using System;
using System.Collections.Generic;

namespace WordGame.Network.Models
{
    [Serializable]
    public class OnlinePlayersResponse
    {
        public List<OnlinePlayerData> players;
    }

    [Serializable]
    public class OnlinePlayerData
    {
        public string PlayerId;
        public string Username;
        public string AvatarUrl;
        public string Status; // "idle" or "in_game"
    }

    [Serializable]
    public class RoomInviteData
    {
        public string inviterId;
        public string inviterName;
        public string inviterAvatar;
        public string roomCode;
    }
}
