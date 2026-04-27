using Microsoft.AspNetCore.Mvc;
using System;
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
        public async Task<ActionResult> GetAllDeadLetterLogsAsync()
        {
            var result = await _deadLetterService.GetAllDeadLetterLogsAsync();
            return Ok(result);
        }

        [HttpGet("unresolved")]
        public async Task<ActionResult> GetUnresolvedDeadLetterLogsAsync()
        {
            var result = await _deadLetterService.GetUnresolvedDeadLetterLogsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetDeadLetterLogByIdAsync([FromRoute] Guid id)
        {
            var result = await _deadLetterService.GetDeadLetterLogByIdAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("{id}/resolve")]
        public async Task<ActionResult> MarkAsResolvedAsync([FromRoute] Guid id, [FromBody] ResolveDeadLetterRequestDTO resolveRequest)
        {
            var result = await _deadLetterService.MarkAsResolvedAsync(id, resolveRequest);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetStatsAsync()
        {
            var result = await _deadLetterService.GetStatsAsync();
            return Ok(result);
        }
    }
}