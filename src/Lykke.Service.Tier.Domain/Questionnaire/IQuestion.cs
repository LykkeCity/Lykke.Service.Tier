namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public interface IQuestion
    {
        string Id { get; }
        string Text { get; }
        QuestionType Type { get; }
        bool Required { get; }
        bool HasOther { get; set; }
        int Order { get; }
    }
}
