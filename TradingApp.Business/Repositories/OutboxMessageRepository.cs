using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingApp.Business.DTOs.Outbox;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities.OutboxMessage;

namespace TradingApp.Business.Repositories
{
    public class OutboxMessageRepository : TradingAppBaseLoggerExtension<OutboxMessageRepository>, IOutboxMessageRepository
    {
        private readonly TradingDbContext _tradingDbContext;

        public OutboxMessageRepository(ITradingAppLogger logger, TradingDbContext tradingDbContext) : base(logger)
        {
            _tradingDbContext = tradingDbContext;
        }

        public async Task<OutboxMessage> GetByIdAsync(Guid id)
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.OutboxMessages
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id);

            LogExitWithScope();

            return result;
        }

        public async Task<IEnumerable<OutboxMessage>> GetAllAsync()
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.OutboxMessages
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            LogExitWithScope();

            return result;
        }

        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync()
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.OutboxMessages
                .AsNoTracking()
                .Where(x => x.ProcessedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            LogExitWithScope();

            return result;
        }

        public async Task<IEnumerable<OutboxMessage>> GetProcessedAsync()
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.OutboxMessages
                .AsNoTracking()
                .Where(x => x.ProcessedAt != null)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            LogExitWithScope();

            return result;
        }

        public async Task<OutboxMessage> MarkAsProcessedAsync(Guid id)
        {
            LogEntryWithScope();

            var outboxMessage = await _tradingDbContext.OutboxMessages
                .SingleOrDefaultAsync(x => x.Id == id);

            if (outboxMessage == null)
            {
                LogExitWithScope();
                return null;
            }

            outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
            await _tradingDbContext.SaveChangesAsync();

            LogExitWithScope();

            return outboxMessage;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            LogEntryWithScope();

            var outboxMessage = await _tradingDbContext.OutboxMessages
                .SingleOrDefaultAsync(x => x.Id == id);

            if (outboxMessage == null)
            {
                LogExitWithScope();
                return false;
            }

            _tradingDbContext.OutboxMessages.Remove(outboxMessage);
            await _tradingDbContext.SaveChangesAsync();

            LogExitWithScope();

            return true;
        }

        public async Task<int> DeleteAllAsync()
        {
            LogEntryWithScope();

            var outboxMessages = await _tradingDbContext.OutboxMessages.ToListAsync();
            var count = outboxMessages.Count;

            if (count > 0)
            {
                _tradingDbContext.OutboxMessages.RemoveRange(outboxMessages);
                await _tradingDbContext.SaveChangesAsync();
            }

            LogExitWithScope();

            return count;
        }

        public async Task<OutboxMessageStatsDTO> GetStatsAsync()
        {
            LogEntryWithScope();

            var allMessages = await _tradingDbContext.OutboxMessages
                .AsNoTracking()
                .ToListAsync();

            var stats = new OutboxMessageStatsDTO
            {
                TotalCount = allMessages.Count,
                ProcessedCount = allMessages.Count(x => x.ProcessedAt.HasValue),
                UnprocessedCount = allMessages.Count(x => !x.ProcessedAt.HasValue),
                Last24Hours = allMessages.Count(x => x.CreatedAt >= DateTimeOffset.UtcNow.AddHours(-24))
            };

            LogExitWithScope();

            return stats;
        }
    }
}
