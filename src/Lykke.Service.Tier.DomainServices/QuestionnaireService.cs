using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.Tier.Domain.Questionnaire;
using Lykke.Service.Tier.Domain.Repositories;
using Lykke.Service.Tier.Domain.Services;

namespace Lykke.Service.Tier.DomainServices
{
    public class QuestionnaireService: IQuestionnaireService
    {
        private readonly IQuestionsRepository _questionsRepository;
        private readonly IAnswersRepository _answersRepository;
        private readonly IUserChoicesRepository _userChoicesRepository;
        private readonly IQuestionsRankRepository _questionsRankRepository;
        private readonly IMapper _mapper;

        public QuestionnaireService(
            IQuestionsRepository questionsRepository,
            IAnswersRepository answersRepository,
            IUserChoicesRepository userChoicesRepository,
            IQuestionsRankRepository questionsRankRepository,
            IMapper mapper
            )
        {
            _questionsRepository = questionsRepository;
            _answersRepository = answersRepository;
            _userChoicesRepository = userChoicesRepository;
            _questionsRankRepository = questionsRankRepository;
            _mapper = mapper;
        }

        public async Task<Question[]> GetQuestionsAsync()
        {
            var result = new List<Question>();
            var questions = await _questionsRepository.GetAllAsync();
            var answers = await _answersRepository.GetAllAsync(questions.Select(x => x.Id).ToArray());

            foreach (var question in questions.OrderBy(x=> x.Order))
            {
                var item = _mapper.Map<Question>(question);
                item.Answers = answers.Where(x => x.QuestionId == question.Id).OrderBy(x => x.Order).ToArray();
                result.Add(item);
            }

            return result.ToArray();
        }

        public Task<IQuestion> GetQuestionAsync(string questionId)
        {
            return _questionsRepository.GetAsync(questionId);
        }

        public Task<IAnswer> GetAnswerAsync(string questionId, string id)
        {
            return _answersRepository.GetAsync(questionId, id);
        }

        public async Task AddQuestionAsync(Question question, string[] answers)
        {
            var questions = await _questionsRepository.GetAllAsync();
            question.Order = questions.Length == 0
                ? 0
                : questions.Length;
            string questionId = await _questionsRepository.AddAsync(question);
            await _answersRepository.AddAnswersAsync(answers.Select((x, i) => new Answer {QuestionId = questionId, Text = x, Order = i}));
        }

        public Task UpdateQuestionAsync(Question question)
        {
            return _questionsRepository.AddAsync(question);
        }

        public Task UpdateAnswersync(Answer answer)
        {
            return _answersRepository.AddAsync(answer);
        }

        public async Task AddAnswersToQuestionAsync(string questionId, string[] answers)
        {
            var answersCount = (await _answersRepository.GetAllAsync(new[] {questionId})).Length;
            await _answersRepository.AddAnswersAsync(answers.Select((x, i) => new Answer {QuestionId = questionId, Text = x, Order = i + answersCount}));
        }

        public async Task DeleteQuestionAsync(string questionId)
        {
            await _questionsRepository.DeleteAsync(questionId);
            await _answersRepository.DeleteAllAsync(questionId);
        }

        public Task DeleteAnswerAsync(string questionId, string id)
        {
            return _answersRepository.DeleteAsync(questionId, id);
        }

        public async Task SaveChoisesAsync(string clientId, Choice[] choices)
        {
            await _userChoicesRepository.AddAsync(clientId, choices);

            var ansserIds = GetAnswerIds(choices);
            var answers = await _answersRepository.GetAllAsync(ansserIds);

            //TODO: calculate rank
            double rank = CalculateRank(answers);

            await SaveQuestionnaireRank(clientId, rank, nameof(QuestionnaireService), "Init calculated rank");
        }

        public async Task<Question[]> GetQuestionnaireAsync(string clientId)
        {
            var choices = await _userChoicesRepository.GetClientChoicesAsync(clientId);
            var questionIds = choices.Select(x => x.QuestionId).ToArray();

            var ids = GetAnswerIds(choices);

            var questionsTask = _questionsRepository.GetAllAsync(questionIds);
            var answersTask = _answersRepository.GetAllAsync(ids);

            await Task.WhenAll(questionsTask, answersTask);

            var result = new List<Question>();

            foreach (var question in questionsTask.Result)
            {
                var answers = answersTask.Result
                    .Where(x => x.QuestionId == question.Id)
                    .OrderBy(x => x.Order).ToList();

                string other = choices.FirstOrDefault(x => x.QuestionId == question.Id)?.Other;

                if (!string.IsNullOrEmpty(other))
                {
                    answers.Add(new Answer{QuestionId = question.Id, Order = int.MaxValue, Text = other});
                }

                result.Add(new Question
                {
                    Id = question.Id,
                    Text = question.Text,
                    Order = question.Order,
                    Answers = answers.ToArray()
                });
            }

            return result.OrderBy(x => x.Order).ToArray();
        }

        public Task SaveQuestionnaireRank(string clientId, double rank, string changer, string comment)
        {
            return _questionsRankRepository.AddAsync(clientId, rank, changer, comment);
        }

        public async Task<IQuestionRank> GetQuestionnaireRankAsync(string clientId)
        {
            return await _questionsRankRepository.GetAsync(clientId);
        }

        public async Task<IReadOnlyList<IQuestionRank>> GetQuestionnaireRanksAsync(string clientId)
        {
            return await _questionsRankRepository.GetAllAsync(clientId);
        }

        private static List<Tuple<string, string>> GetAnswerIds(IChoice[] choices)
        {
            var ids = new List<Tuple<string, string>>();

            foreach (var choice in choices)
            {
                ids.AddRange(choice.AnswerIds.Select(answerId => new Tuple<string, string>(choice.QuestionId, answerId)));
            }

            return ids;
        }

        private double CalculateRank(IAnswer[] answers)
        {
            return answers.Sum(x => x.Weight);
        }
    }
}
