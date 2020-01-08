using System;

namespace Lykke.Service.Tier.Domain.Questionnaire
{
    public interface IQuestionRank
    {
        string ClientId { get; }
        double Rank { get; }
        string Changer { get; }
        string Comment { get; }
        DateTime CreatedAt { get; }
    }
}
