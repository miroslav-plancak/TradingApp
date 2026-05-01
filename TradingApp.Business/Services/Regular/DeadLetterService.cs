using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingApp.Business.DTOs;
using TradingApp.Business.DTOs.DeadLetter;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Business.Mappers;

namespace TradingApp.Business.Services.Regular
{
    public class DeadLetterService : TradingAppBaseLoggerExtension<DeadLetterService>, IDeadLetterService
    {
        private readonly IDeadLetterRepository _deadLetterRepository;

        public DeadLetterService(
            ITradingAppLogger logger,
            IDeadLetterRepository deadLetterRepository) : base(logger)
        {
            _deadLetterRepository = deadLetterRepository;
        }

        public async Task<DeadLetterLogResponseDTO> CreateDeadLetterLogAsync(string messageBody, Guid clientOrderId, string reason)
        {
            LogEntryWithScope();

            var deadLetterEntity = DeadLetterMapper.ToEntity(messageBody, clientOrderId, reason);
            var deadLetterLog = await _deadLetterRepository.CreateDeadLetterLogAsync(deadLetterEntity);

            LogExitWithScope();

            return DeadLetterMapper.ToDeadLetterLogResponseDTO(deadLetterLog);
        }

        public Task<DeadLetterLogResponseDTO> CreateDeadLetterLogAsync(CreateDeadLetterRequestDTO createRequest)
        {
            return CreateDeadLetterLogAsync(createRequest.MessageBody, createRequest.ClientOrderId, createRequest.Reason);
        }

        public async Task<DeadLetterLogResponseDTO> GetByClientOrderIdAsync(Guid clientOrderId)
        {
            LogEntryWithScope();

            var deadLetterLog = await _deadLetterRepository.GetByClientOrderIdAsync(clientOrderId);
            var deadLetterDTO = DeadLetterMapper.ToDeadLetterLogResponseDTO(deadLetterLog);

            LogExitWithScope();

            return deadLetterDTO;
        }

        public async Task<bool> DeleteDeadLetterLogAsync(Guid id)
        {
            LogEntryWithScope();

            var deleted = await _deadLetterRepository.DeleteDeadLetterLogAsync(id);

            LogExitWithScope();

            return deleted;
        }

        public async Task<int> DeleteAllDeadLetterLogsAsync()
        {
            LogEntryWithScope();

            var deletedCount = await _deadLetterRepository.DeleteAllDeadLetterLogsAsync();

            LogExitWithScope();

            return deletedCount;
        }


        public async Task<DeadLetterLogResponseDTO> GetDeadLetterLogByIdAsync(Guid id)
        {
            LogEntryWithScope();

            var deadLetterLog = await _deadLetterRepository.GetDeadLetterLogByIdAsync(id);
            var deadLetterDTO = DeadLetterMapper.ToDeadLetterLogResponseDTO(deadLetterLog);

            LogExitWithScope();

            return deadLetterDTO;
        }

        public async Task<IEnumerable<DeadLetterLogResponseDTO>> GetAllDeadLetterLogsAsync()
        {
            LogEntryWithScope();

            var deadLetterLogs = await _deadLetterRepository.GetAllDeadLetterLogsAsync();
            var deadLetterDTOs = DeadLetterMapper.ToDeadLetterLogResponseDTOs(deadLetterLogs);

            LogExitWithScope();

            return deadLetterDTOs;
        }

        public async Task<IEnumerable<DeadLetterLogResponseDTO>> GetUnresolvedDeadLetterLogsAsync()
        {
            LogEntryWithScope();

            var deadLetterLogs = await _deadLetterRepository.GetUnresolvedDeadLetterLogsAsync();
            var deadLetterDTOs = DeadLetterMapper.ToDeadLetterLogResponseDTOs(deadLetterLogs);

            LogExitWithScope();

            return deadLetterDTOs;
        }

        public async Task<DeadLetterLogResponseDTO> MarkAsResolvedAsync(Guid id, ResolveDeadLetterRequestDTO resolveRequest)
        {
            LogEntryWithScope();

            var deadLetterLog = await _deadLetterRepository.MarkAsResolvedAsync(
                id,
                resolveRequest.ResolutionNotes,
                resolveRequest.ResolvedBy);

            var deadLetterDTO = DeadLetterMapper.ToDeadLetterLogResponseDTO(deadLetterLog);

            LogExitWithScope();

            return deadLetterDTO;
        }

        public async Task<DeadLetterStatsDTO> GetStatsAsync()
        {
            LogEntryWithScope();

            var stats = await _deadLetterRepository.GetStatsAsync();

            LogExitWithScope();

            return stats;
        }

        public async Task MarkOutboxMessageAsProcessedAsync(Guid clientOrderId)
        {
            LogEntryWithScope();

            await _deadLetterRepository.MarkOutboxMessageAsProcessedAsync(clientOrderId);

            LogExitWithScope();
        }
    }
}