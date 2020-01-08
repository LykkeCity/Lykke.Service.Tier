namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public interface IChoice
    {
        string QuestionId { get; }
        string[] AnswerIds { get; }
        string Other { get; }
    }
}
