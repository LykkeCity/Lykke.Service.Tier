using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models;
using Lykke.Service.Tier.Client.Models.Requests;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain.Questionnaire;
using Lykke.Service.Tier.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/questionnaire")]
    public class QuestionnaireController : Controller, IQuestionnaireApi
    {
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IMapper _mapper;

        public QuestionnaireController(
            IQuestionnaireService questionnaireService,
            IMapper mapper

            )
        {
            _questionnaireService = questionnaireService;
            _mapper = mapper;
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpGet]
        [SwaggerOperation("GetQuestionnaire")]
        [ProducesResponseType(typeof(QuestionnaireResponse), (int)HttpStatusCode.OK)]
        public async Task<QuestionnaireResponse> GetQuestionnaireAsync()
        {
            Question[] questions = await _questionnaireService.GetQuestionsAsync();

            var result = new QuestionnaireResponse {Questionnaire = _mapper.Map<QuestionModel[]>(questions)};

            return result;
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpGet("answered/{clientId}")]
        [SwaggerOperation("GetQuestionnaire")]
        [ProducesResponseType(typeof(QuestionnaireResponse), (int)HttpStatusCode.OK)]
        public async Task<FilledQuestionnaireResponse> GetAnsweredQuestionnaireAsync(string clientId)
        {
            var questionsTask = _questionnaireService.GetQuestionnaireAsync(clientId);
            var rankTask = _questionnaireService.GetQuestionnaireRankAsync(clientId);

            await Task.WhenAll(questionsTask, rankTask);

            var result = new FilledQuestionnaireResponse
            {
                Questionnaire = _mapper.Map<AnsweredQuestionModel[]>(questionsTask.Result),
                Rank = rankTask.Result?.Rank ?? 0
            };

            return result;
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpPost("choices")]
        [SwaggerOperation("SaveChoices")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        public Task SaveChoicesAsync([FromBody]ChoicesRequest model)
        {
            return _questionnaireService.SaveChoisesAsync(model.ClientId, _mapper.Map<Choice[]>(model.Choices));
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpPost("question")]
        [SwaggerOperation("AddQuestion")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public Task AddQuestionAsync([FromBody]QuestionRequest model)
        {
            return _questionnaireService.AddQuestionAsync(_mapper.Map<Question>(model), model.Answers);
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpPut("question")]
        [SwaggerOperation("UpdateQuestion")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task UpdateQuestionAsync([FromBody]QuestionUpdateRequest model)
        {
            var question = await _questionnaireService.GetQuestionAsync(model.Id);

            if (question != null)
            {
                await _questionnaireService.UpdateQuestionAsync(_mapper.Map<Question>(model));
            }
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpPut("question/{questionId}/answers")]
        [SwaggerOperation("AddAnswersToQuestion")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task AddAnswersToQuestionAsync(string questionId, [FromBody]string[] answers)
        {
            var question = await _questionnaireService.GetQuestionAsync(questionId);

            if (question != null)
            {
                await _questionnaireService.AddAnswersToQuestionAsync(questionId, answers);
            }
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpPut("answer")]
        [SwaggerOperation("UpdateAnswer")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task UpdateAnswerAsync([FromBody]AnswerUpdateRequest model)
        {
            var answer = await _questionnaireService.GetAnswerAsync(model.QuestionId, model.Id);

            if (answer != null)
            {
                await _questionnaireService.UpdateAnswersync(_mapper.Map<Answer>(model));
            }
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpDelete("question/{questionId}")]
        [SwaggerOperation("DeleteQuestion")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task DeleteQuestionAsync(string questionId)
        {
            var question = await _questionnaireService.GetQuestionAsync(questionId);

            if (question != null)
            {
                await _questionnaireService.DeleteQuestionAsync(questionId);
            }
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpDelete("question/{questionId}/{answerId}")]
        [SwaggerOperation("DeleteAnswer")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task DeleteAnswerAsync(string questionId, string answerId)
        {
            var question = await _questionnaireService.GetQuestionAsync(questionId);

            if (question != null)
            {
                await _questionnaireService.DeleteAnswerAsync(questionId, answerId);
            }
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpPost("rank")]
        [SwaggerOperation("SaveQuestionnaireRank")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public Task SaveQuestionnaireRankAsync([FromBody]QuestionnaireRankRequest model)
        {
            return _questionnaireService.SaveQuestionnaireRank(model.ClientId, model.Rank, model.Changer, model.Comment);
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpGet("rank/{clientId}")]
        [SwaggerOperation("GetQuestionnaireRank")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<QuestionnaireRankResponse> GetQuestionnaireRankAsync(string clientId)
        {
            var rank = await _questionnaireService.GetQuestionnaireRankAsync(clientId);
            return _mapper.Map<QuestionnaireRankResponse>(rank);
        }

        /// <inheritdoc cref="IQuestionnaireApi"/>
        [HttpGet("rank/{clientId}/all")]
        [SwaggerOperation("GetQuestionnaireRanks")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyList<QuestionnaireRankResponse>> GetQuestionnaireRanksAsync(string clientId)
        {
            var ranks = await _questionnaireService.GetQuestionnaireRanksAsync(clientId);
            return _mapper.Map<IReadOnlyList<QuestionnaireRankResponse>>(ranks);
        }
    }
}
