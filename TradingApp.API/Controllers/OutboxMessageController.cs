using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Services;

namespace TradingApp.API.Controllers
{
    public class OutboxMessageController : TradingAppBaseController<OutboxMessageController>
    {
        private readonly IOutboxMessageService _outboxMessageService;

        public OutboxMessageController(ITradingAppLogger logger, IOutboxMessageService outboxMessageService) : base(logger)
        {
            _outboxMessageService = outboxMessageService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAsync()
        {
            var result = await _outboxMessageService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("unprocessed")]
        public async Task<ActionResult> GetUnprocessedAsync()
        {
            var result = await _outboxMessageService.GetUnprocessedAsync();
            return Ok(result);
        }

        [HttpGet("processed")]
        public async Task<ActionResult> GetProcessedAsync()
        {
            var result = await _outboxMessageService.GetProcessedAsync();
            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetStatsAsync()
        {
            var result = await _outboxMessageService.GetStatsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetByIdAsync([FromRoute] Guid id)
        {
            var result = await _outboxMessageService.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("{id}/mark-processed")]
        public async Task<ActionResult> MarkAsProcessedAsync([FromRoute] Guid id)
        {
            var result = await _outboxMessageService.MarkAsProcessedAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync([FromRoute] Guid id)
        {
            var deleted = await _outboxMessageService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteAllAsync()
        {
            var deletedCount = await _outboxMessageService.DeleteAllAsync();
            return Ok(new { deletedCount });
        }
    }
}
