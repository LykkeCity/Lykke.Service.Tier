using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Extensions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TierUpgradeRequest = Lykke.Service.Tier.Client.Models.Requests.TierUpgradeRequest;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/tierupgraderequests")]
    public class TierUpgradeRequestsController : Controller, ITierUpgradeRequestsApi
    {
        private readonly ITierUpgradeService _tierUpgradeService;
        private readonly IMapper _mapper;

        public TierUpgradeRequestsController(
            ITierUpgradeService tierUpgradeService,
            IMapper mapper
            )
        {
            _tierUpgradeService = tierUpgradeService;
            _mapper = mapper;
        }
        /// <inheritdoc cref="ITierUpgradeRequestsApi"/>
        [HttpGet("count")]
        [SwaggerOperation("TierUpgradeRequestsCount")]
        [ProducesResponseType(typeof(Dictionary<string, int>), (int)HttpStatusCode.OK)]
        public Task<Dictionary<string, int>> GetCountsAsync()
        {
            return _tierUpgradeService.GetCountsAsync();
        }

        /// <inheritdoc cref="ITierUpgradeRequestsApi"/>
        [HttpGet("{clientId}/{tier}")]
        [SwaggerOperation("TierUpgradeRequest")]
        [ProducesResponseType(typeof(TierUpgradeRequestResponse), (int)HttpStatusCode.OK)]
        public async Task<TierUpgradeRequestResponse> GetAsync(string clientId, TierModel tier)
        {
            ITierUpgradeRequest result = await _tierUpgradeService.GetAsync(clientId, tier.ToAccountTier());

            return _mapper.Map<TierUpgradeRequestResponse>(result);
        }

        /// <inheritdoc cref="ITierUpgradeRequestsApi"/>
        [HttpGet]
        [SwaggerOperation("TierUpgradeRequestsAll")]
        [ProducesResponseType(typeof(IReadOnlyList<TierUpgradeRequestResponse>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyList<TierUpgradeRequestResponse>> GetAllAsync()
        {
            var result = await _tierUpgradeService.GetAllAsync();
            return _mapper.Map<List<TierUpgradeRequestResponse>>(result);
        }

        /// <inheritdoc cref="ITierUpgradeRequestsApi"/>
        [HttpGet("client/{clientId}")]
        [SwaggerOperation("TierUpgradeRequests")]
        [ProducesResponseType(typeof(IReadOnlyList<TierUpgradeRequestResponse>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyList<TierUpgradeRequestResponse>> GetByClientAsync(string clientId)
        {
            var result = await _tierUpgradeService.GetByClientAsync(clientId);
            return _mapper.Map<List<TierUpgradeRequestResponse>>(result);
        }

        /// <inheritdoc cref="ITierUpgradeRequestsApi"/>
        [HttpGet("{tier}")]
        [SwaggerOperation("TierUpgradeRequests")]
        [ProducesResponseType(typeof(IReadOnlyList<TierUpgradeRequestResponse>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyList<TierUpgradeRequestResponse>> GetByTierAsync(TierModel tier)
        {
            var result = await _tierUpgradeService.GetByTierAsync(tier.ToAccountTier());
            return _mapper.Map<IReadOnlyList<TierUpgradeRequestResponse>>(result.Where(x => x.KycStatus != KycStatus.Ok));
        }

        /// <inheritdoc cref="ITierUpgradeRequestsApi"/>
        [HttpPost]
        [SwaggerOperation("TierUpgradeRequest")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public Task AddAsync([FromBody]TierUpgradeRequest request)
        {
            Enum.TryParse(request.KycStatus, out KycStatus status);

            return _tierUpgradeService.AddAsync(request.ClientId, request.Tier.ToAccountTier(), status,
                request.Changer, request.Comment);
        }
    }
}
