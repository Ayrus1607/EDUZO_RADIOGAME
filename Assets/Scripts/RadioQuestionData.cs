using System;

// strict namespace rule from Eduzo
namespace Eduzo.Games.Radio.Data
{
    [Serializable]
    public class RadioQuestionData
    {
        public string questionText;
        public string correctAnswer;
        public string wrongAnswer;
    }
}