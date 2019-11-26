namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public class Question : IQuestion
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public bool Required { get; set; }
        public bool HasOther { get; set; }
        public int Order { get; set; }

        public IAnswer[] Answers { get; set; }
    }
}
