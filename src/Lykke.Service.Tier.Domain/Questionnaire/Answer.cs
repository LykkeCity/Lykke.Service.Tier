namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public class Answer : IAnswer
    {
        public string Id { get; set; }
        public string QuestionId { get; set; }
        public string Text { get; set; }
        public int Order { get; set; }
        public double Weight { get; set; }
    }
}
