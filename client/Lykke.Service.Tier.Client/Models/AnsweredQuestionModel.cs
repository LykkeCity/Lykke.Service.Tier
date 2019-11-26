namespace Lykke.Service.Tier.Client.Models
{
    public class AnsweredQuestionModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public AnswerModel[] Answers { get; set; }
    }
}
