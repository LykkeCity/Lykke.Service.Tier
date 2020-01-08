namespace Lykke.Service.Tier.Client.Models.Responses
{
    public class FilledQuestionnaireResponse
    {
        public double Rank { get; set; }
        public AnsweredQuestionModel[] Questionnaire { get; set; }
    }
}
