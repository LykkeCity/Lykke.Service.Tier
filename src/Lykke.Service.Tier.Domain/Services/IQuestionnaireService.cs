using System.Threading.Tasks;
using Lykke.Service.Tier.Domain.Questionnaire;

namespace Lykke.Service.Tier.Domain.Services
{
    public interface IQuestionnaireService
    {
        Task<Question[]> GetQuestionsAsync();
        Task<IQuestion> GetQuestionAsync(string questionId);
        Task<IAnswer> GetAnswerAsync(string questionId, string id);
        Task AddQuestionAsync(Question question, string[] answers);
        Task UpdateQuestionAsync(Question question);
        Task UpdateAnswersync(Answer answer);
        Task AddAnswersToQuestionAsync(string questionId, string[] answers);
        Task DeleteQuestionAsync(string questionId);
        Task DeleteAnswerAsync(string questionId, string id);
        Task SaveChoisesAsync(string clientId, Choice[] choices);
        Task<Question[]> GetQuestionnaireAsync(string clientId);
    }
}
