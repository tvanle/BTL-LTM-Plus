using System;
using System.Collections.Generic;

namespace WordGame.Network.Models
{
    [Serializable]
    public class GameStartData
    {
        public string category;
        public int level;
        public int duration;
        public string gridData;  // Serialized grid/board data for the level
        public List<string> targetWords; // Words to find in this level
    }
}