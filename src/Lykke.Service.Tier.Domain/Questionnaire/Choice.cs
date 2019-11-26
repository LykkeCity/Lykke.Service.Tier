namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public class Choice : IChoice
    {
        public string QuestionId { get; set; }
        public string[] AnswerIds { get; set; }
        public string Other { get; set; }
    }
}
