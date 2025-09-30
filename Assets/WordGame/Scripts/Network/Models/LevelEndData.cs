using System;
using System.Collections.Generic;

namespace WordGame.Network.Models
{
    [Serializable]
    public class LevelEndData
    {
        public int level;
        public List<PlayerResult> results;
    }
}