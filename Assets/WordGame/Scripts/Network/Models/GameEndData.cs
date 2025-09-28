using System;
using System.Collections.Generic;

namespace WordGame.Network.Models
{
    [Serializable]
    public class GameEndData
    {
        public List<PlayerResult> results;
    }

    [Serializable]
    public class PlayerResult
    {
        public string Id;
        public string Username;
        public int Score;
    }
}