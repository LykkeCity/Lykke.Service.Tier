namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public interface IAnswer
    {
        string Id { get; }
        string QuestionId { get; }
        string Text { get; }
        int Order { get; }
        double Weight { get; }
    }
}
