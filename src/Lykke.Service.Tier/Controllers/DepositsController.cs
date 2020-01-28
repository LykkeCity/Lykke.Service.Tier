using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.Tier.Client.Api;
using Lykke.Service.Tier.Client.Models.Responses;
using Lykke.Service.Tier.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.Tier.Controllers
{
    [Route("api/deposits")]
    public class DepositsController : Controller, IDepositsApi
    {
        private readonly ILimitsService _limitsService;
        private readonly IMapper _mapper;

        public DepositsController(
            ILimitsService limitsService,
            IMapper mapper
            )
        {
            _limitsService = limitsService;
            _mapper = mapper;
        }

        /// <inheritdoc cref="IDepositsApi"/>
        [HttpGet("{clientId}")]
        [SwaggerOperation("GetClientDeposits")]
        [ProducesResponseType(typeof(DepositsResponse), (int)HttpStatusCode.OK)]
        public async Task<DepositsResponse> GetClientDepositsAsync(string clientId)
        {
            var deposits = await _limitsService.GetClientDepositsAsync(clientId);

            return new DepositsResponse {Deposits = _mapper.Map<IReadOnlyCollection<ClientDepositOperation>>(deposits)};
        }

        /// <inheritdoc cref="IDepositsApi"/>
        [HttpDelete("{clientId}/{operationId}")]
        [SwaggerOperation("DeleteClientDeposit")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public Task DeleteClientDepositAsync(string clientId, string operationId)
        {
            return _limitsService.DeleteDepositOperationAsync(clientId, operationId);
        }
    }
}
