using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingApp.Business.DTOs;
using TradingApp.Business.DTOs.DeadLetter;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Repositories
{
    public class DeadLetterRepository : TradingAppBaseLoggerExtension<DeadLetterRepository>, IDeadLetterRepository
    {
        private readonly TradingDbContext _tradingDbContext;

        public DeadLetterRepository(ITradingAppLogger logger, TradingDbContext tradingDbContext) : base(logger)
        {
            _tradingDbContext = tradingDbContext;
        }

        public async Task<DeadLetterLog> CreateDeadLetterLogAsync(DeadLetterLog deadLetterLog)
        {
            LogEntryWithScope();

            deadLetterLog.Id = Guid.NewGuid();
            deadLetterLog.CreatedAt = DateTimeOffset.UtcNow;
            deadLetterLog.IsResolved = false;

            _tradingDbContext.DeadLetterLogs.Add(deadLetterLog);
            await _tradingDbContext.SaveChangesAsync();

            LogExitWithScope();

            return deadLetterLog;
        }

        public async Task<DeadLetterLog> GetDeadLetterLogByIdAsync(Guid id)
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.DeadLetterLogs
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id);

            LogExitWithScope();

            return result;
        }

        public async Task<IEnumerable<DeadLetterLog>> GetAllDeadLetterLogsAsync()
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.DeadLetterLogs
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            LogExitWithScope();

            return result;
        }

        public async Task<IEnumerable<DeadLetterLog>> GetUnresolvedDeadLetterLogsAsync()
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.DeadLetterLogs
                .Where(x => !x.IsResolved)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            LogExitWithScope();

            return result;
        }

        public async Task<DeadLetterLog> MarkAsResolvedAsync(Guid id, string resolutionNotes, string resolvedBy)
        {
            LogEntryWithScope();

            var deadLetterLog = await _tradingDbContext.DeadLetterLogs
                .SingleOrDefaultAsync(x => x.Id == id);

            if (deadLetterLog == null)
            {
                LogExitWithScope();
                return null;
            }

            deadLetterLog.IsResolved = true;
            deadLetterLog.ResolutionNotes = resolutionNotes;
            deadLetterLog.ResolvedBy = resolvedBy;
            deadLetterLog.ResolvedAt = DateTimeOffset.UtcNow;

            await _tradingDbContext.SaveChangesAsync();

            LogExitWithScope();

            return deadLetterLog;
        }

        public async Task<DeadLetterLog> GetByClientOrderIdAsync(Guid clientOrderId)
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.DeadLetterLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientOrderId == clientOrderId);

            LogExitWithScope();

            return result;
        }

        public async Task<DeadLetterStatsDTO> GetStatsAsync()
        {
            LogEntryWithScope();

            var allDeadLetters = await _tradingDbContext.DeadLetterLogs.ToListAsync();

            var stats = new DeadLetterStatsDTO
            {
                TotalCount = allDeadLetters.Count,
                UnresolvedCount = allDeadLetters.Count(x => !x.IsResolved),
                ResolvedCount = allDeadLetters.Count(x => x.IsResolved),
                Last24Hours = allDeadLetters.Count(x => x.CreatedAt >= DateTimeOffset.UtcNow.AddHours(-24))
            };

            LogExitWithScope();

            return stats;
        }

        public async Task MarkOutboxMessageAsProcessedAsync(Guid clientOrderId)
        {
            LogEntryWithScope();

            var outboxMessage = await _tradingDbContext.OutboxMessages
                .FirstOrDefaultAsync(x =>
                    x.Payload == clientOrderId.ToString() &&
                    x.ProcessedAt == null);

            if (outboxMessage != null)
            {
                outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                await _tradingDbContext.SaveChangesAsync();
            }

            LogExitWithScope();
        }

        public async Task<bool> DeleteDeadLetterLogAsync(Guid id)
        {
            LogEntryWithScope();

            var deadLetterLog = await _tradingDbContext.DeadLetterLogs
                .SingleOrDefaultAsync(x => x.Id == id);

            if (deadLetterLog == null)
            {
                LogExitWithScope();
                return false;
            }

            _tradingDbContext.DeadLetterLogs.Remove(deadLetterLog);
            await _tradingDbContext.SaveChangesAsync();

            LogExitWithScope();

            return true;
        }

        public async Task<int> DeleteAllDeadLetterLogsAsync()
        {
            LogEntryWithScope();

            var deadLetterLogs = await _tradingDbContext.DeadLetterLogs.ToListAsync();
            var count = deadLetterLogs.Count;

            if (count > 0)
            {
                _tradingDbContext.DeadLetterLogs.RemoveRange(deadLetterLogs);
                await _tradingDbContext.SaveChangesAsync();
            }

            LogExitWithScope();

            return count;
        }
    }
}