using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.Service.Tier.Client.Models.Responses;
using Refit;

namespace Lykke.Service.Tier.Client.Api
{
    /// <summary>
    /// Questionnaire API interface.
    /// </summary>
    [PublicAPI]
    public interface IQuestionnaireApi
    {
        /// <summary>
        /// Gets questionnaire
        /// </summary>
        /// <returns></returns>
        [Get("/api/questionnaire")]
        Task<QuestionnaireResponse> GetQuestionnaireAsync();

        /// <summary>
        /// Gets answered questionnaire
        /// </summary>
        /// <returns></returns>
        [Get("/api/questionnaire/answered/{clientId}")]
        Task<FilledQuestionnaireResponse> GetAnsweredQuestionnaireAsync(string clientId);

        /// <summary>
        /// Saves user questionnaire choices
        /// </summary>
        /// <param name="model">cho</param>
        /// <returns></returns>
        [Post("/api/questionnaire/choices")]
        Task SaveChoicesAsync(ChoicesRequest model);

        /// <summary>
        /// Adds question
        /// </summary>
        /// <param name="model">question model</param>
        /// <returns></returns>
        [Post("/api/questionnaire/question")]
        Task AddQuestionAsync([Body] QuestionRequest model);

        /// <summary>
        /// Updates question
        /// </summary>
        /// <param name="model">answers</param>
        /// <returns></returns>
        [Put("/api/questionnaire/question")]
        Task UpdateQuestionAsync([Body] QuestionUpdateRequest model);

        /// <summary>
        /// Adds answers to the question
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <param name="answers">answers</param>
        /// <returns></returns>
        [Put("/api/questionnaire/question/{questionId}/answers")]
        Task AddAnswersToQuestionAsync(string questionId, [Body] string[] answers);

        /// <summary>
        /// Updates answer
        /// </summary>
        /// <param name="model">answer</param>
        /// <returns></returns>
        [Put("/api/questionnaire/answer")]
        Task UpdateAnswerAsync([Body] AnswerUpdateRequest model);

        /// <summary>
        /// Deletes question
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <returns></returns>
        [Delete("/api/questionnaire/question/{questionId}")]
        Task DeleteQuestionAsync(string questionId);

        /// <summary>
        /// Deletes question answer
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <param name="answerId">answer id</param>
        /// <returns></returns>
        [Delete("/api/questionnaire/question/{questionId}/{answerId}")]
        Task DeleteAnswerAsync(string questionId, string answerId);

        /// <summary>
        /// Saves questionnaire rank
        /// </summary>
        /// <param name="model">rank model</param>
        /// <returns></returns>
        [Post("/api/questionnaire/rank")]
        Task SaveQuestionnaireRankAsync([Body]QuestionnaireRankRequest model);

        /// <summary>
        /// Gets client questionnaire ranks
        /// </summary>
        /// <returns></returns>
        [Get("/api/questionnaire/rank/{clientId}")]
        Task<QuestionnaireRankResponse> GetQuestionnaireRankAsync(string clientId);

        /// <summary>
        /// Gets all client questionnaire ranks
        /// </summary>
        /// <returns></returns>
        [Get("/api/questionnaire/rank/{clientId}/all")]
        Task<IReadOnlyList<QuestionnaireRankResponse>> GetQuestionnaireRanksAsync(string clientId);
    }
}

