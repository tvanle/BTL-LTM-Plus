using System;
using System.Collections.Generic;

namespace WordGame.Network.Models
{
    [Serializable]
    public class GameStartData
    {
        public int level;
        public int duration;
    }
}