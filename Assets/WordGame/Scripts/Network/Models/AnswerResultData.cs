using System;

namespace WordGame.Network.Models
{
    [Serializable]
    public class AnswerResultData
    {
        public string playerId;
        public string answer;
        public bool isCorrect;
        public int score;
    }
}