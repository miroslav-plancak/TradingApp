using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingApp.Business.DTOs.Outbox;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Business.Mappers;

namespace TradingApp.Business.Services.Regular
{
    public class OutboxMessageService : TradingAppBaseLoggerExtension<OutboxMessageService>, IOutboxMessageService
    {
        private readonly IOutboxMessageRepository _outboxMessageRepository;

        public OutboxMessageService(
            ITradingAppLogger logger,
            IOutboxMessageRepository outboxMessageRepository) : base(logger)
        {
            _outboxMessageRepository = outboxMessageRepository;
        }

        public async Task<OutboxMessageResponseDTO> GetByIdAsync(Guid id)
        {
            LogEntryWithScope();

            var entity = await _outboxMessageRepository.GetByIdAsync(id);
            var dto = OutboxMessageMapper.ToOutboxMessageResponseDTO(entity);

            LogExitWithScope();

            return dto;
        }

        public async Task<IEnumerable<OutboxMessageResponseDTO>> GetAllAsync()
        {
            LogEntryWithScope();

            var entities = await _outboxMessageRepository.GetAllAsync();
            var dtos = OutboxMessageMapper.ToOutboxMessageResponseDTOs(entities);

            LogExitWithScope();

            return dtos;
        }

        public async Task<IEnumerable<OutboxMessageResponseDTO>> GetUnprocessedAsync()
        {
            LogEntryWithScope();

            var entities = await _outboxMessageRepository.GetUnprocessedAsync();
            var dtos = OutboxMessageMapper.ToOutboxMessageResponseDTOs(entities);

            LogExitWithScope();

            return dtos;
        }

        public async Task<IEnumerable<OutboxMessageResponseDTO>> GetProcessedAsync()
        {
            LogEntryWithScope();

            var entities = await _outboxMessageRepository.GetProcessedAsync();
            var dtos = OutboxMessageMapper.ToOutboxMessageResponseDTOs(entities);

            LogExitWithScope();

            return dtos;
        }

        public async Task<OutboxMessageResponseDTO> MarkAsProcessedAsync(Guid id)
        {
            LogEntryWithScope();

            var entity = await _outboxMessageRepository.MarkAsProcessedAsync(id);
            var dto = OutboxMessageMapper.ToOutboxMessageResponseDTO(entity);

            LogExitWithScope();

            return dto;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            LogEntryWithScope();

            var deleted = await _outboxMessageRepository.DeleteAsync(id);

            LogExitWithScope();

            return deleted;
        }

        public async Task<int> DeleteAllAsync()
        {
            LogEntryWithScope();

            var deletedCount = await _outboxMessageRepository.DeleteAllAsync();

            LogExitWithScope();

            return deletedCount;
        }

        public async Task<OutboxMessageStatsDTO> GetStatsAsync()
        {
            LogEntryWithScope();

            var stats = await _outboxMessageRepository.GetStatsAsync();

            LogExitWithScope();

            return stats;
        }
    }
}
