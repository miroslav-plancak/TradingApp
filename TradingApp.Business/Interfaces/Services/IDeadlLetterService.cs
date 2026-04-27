using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingApp.Business.DTOs;
using TradingApp.Business.DTOs.DeadLetter;

namespace TradingApp.Business.Interfaces.Services
{
    public interface IDeadLetterService
    {
        Task<DeadLetterLogResponseDTO> CreateDeadLetterLogAsync(string messageBody, Guid clientOrderId, string reason);
        Task<DeadLetterLogResponseDTO> GetDeadLetterLogByIdAsync(Guid id);
        Task<IEnumerable<DeadLetterLogResponseDTO>> GetAllDeadLetterLogsAsync();
        Task<IEnumerable<DeadLetterLogResponseDTO>> GetUnresolvedDeadLetterLogsAsync();
        Task<DeadLetterLogResponseDTO> MarkAsResolvedAsync(Guid id, ResolveDeadLetterRequestDTO resolveRequest);
        Task<DeadLetterStatsDTO> GetStatsAsync();
        Task MarkOutboxMessageAsProcessedAsync(Guid clientOrderId);
    }
}