using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingApp.Business.DTOs.DeadLetter;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Services;

namespace TradingApp.API.Controllers
{
    public class DeadLetterController : TradingAppBaseController<DeadLetterController>
    {
        private readonly IDeadLetterService _deadLetterService;

        public DeadLetterController(ITradingAppLogger logger, IDeadLetterService deadLetterService) : base(logger)
        {
            _deadLetterService = deadLetterService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DeadLetterLogResponseDTO>), 200)]
        public async Task<ActionResult> GetAllDeadLetterLogsAsync()
        {
            var result = await _deadLetterService.GetAllDeadLetterLogsAsync();
            return Ok(result);
        }

        [HttpGet("unresolved")]
        [ProducesResponseType(typeof(IEnumerable<DeadLetterLogResponseDTO>), 200)]
        public async Task<ActionResult> GetUnresolvedDeadLetterLogsAsync()
        {
            var result = await _deadLetterService.GetUnresolvedDeadLetterLogsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DeadLetterLogResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetDeadLetterLogByIdAsync([FromRoute] Guid id)
        {
            var result = await _deadLetterService.GetDeadLetterLogByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("{id}/resolve")]
        [ProducesResponseType(typeof(DeadLetterLogResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> MarkAsResolvedAsync([FromRoute] Guid id, [FromBody] ResolveDeadLetterRequestDTO resolveRequest)
        {
            var result = await _deadLetterService.MarkAsResolvedAsync(id, resolveRequest);
            return Ok(result);
        }

        [HttpGet("stats")]
        [ProducesResponseType(typeof(DeadLetterStatsDTO), 200)]
        public async Task<ActionResult> GetStatsAsync()
        {
            var result = await _deadLetterService.GetStatsAsync();
            return Ok(result);
        }

        [HttpGet("by-client-order/{clientOrderId}")]
        [ProducesResponseType(typeof(DeadLetterLogResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetByClientOrderIdAsync([FromRoute] Guid clientOrderId)
        {
            var result = await _deadLetterService.GetByClientOrderIdAsync(clientOrderId);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DeadLetterLogResponseDTO), 200)]
        public async Task<ActionResult> CreateDeadLetterLogAsync([FromBody] CreateDeadLetterRequestDTO createRequest)
        {
            var result = await _deadLetterService.CreateDeadLetterLogAsync(createRequest);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(DeadLetterLogResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteDeadLetterLogAsync([FromRoute] Guid id)
        {
            var result = await _deadLetterService.DeleteDeadLetterLogAsync(id);
            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<ActionResult> DeleteAllDeadLetterLogsAsync()
        {
            var deletedCount = await _deadLetterService.DeleteAllDeadLetterLogsAsync();
            return Ok(new { deletedCount });
        }
    }
}
