using System;

namespace WordGame.Network.Models
{
    [Serializable]
    public class GameStartData
    {
        public string category;
        public int level;
    }
}