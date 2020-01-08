namespace Lykke.Service.Tier.Client.Models
{
    public class QuestionModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public QuestionTypeModel Type { get; set; }
        public bool Required { get; set; }
        public bool HasOther { get; set; }
        public AnswerModel[] Answers { get; set; }
    }
}
