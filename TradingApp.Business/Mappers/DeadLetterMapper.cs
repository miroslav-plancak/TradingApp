using System.Collections.Generic;
using System.Linq;
using TradingApp.Business.DTOs;
using TradingApp.Business.DTOs.DeadLetter;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Mappers
{
    public static class DeadLetterMapper
    {
        public static DeadLetterLogResponseDTO ToDeadLetterLogResponseDTO(DeadLetterLog deadLetterLog)
        {
            if (deadLetterLog == null) return null;

            return new DeadLetterLogResponseDTO
            {
                Id = deadLetterLog.Id,
                ClientOrderId = deadLetterLog.ClientOrderId,
                Reason = deadLetterLog.Reason,
                CreatedAt = deadLetterLog.CreatedAt,
                IsResolved = deadLetterLog.IsResolved,
                ResolutionNotes = deadLetterLog.ResolutionNotes,
                ResolvedAt = deadLetterLog.ResolvedAt,
                ResolvedBy = deadLetterLog.ResolvedBy,
                MessageBody = deadLetterLog.MessageBody
            };
        }

        public static IEnumerable<DeadLetterLogResponseDTO> ToDeadLetterLogResponseDTOs(IEnumerable<DeadLetterLog> deadLetterLogs)
        {
            if (deadLetterLogs == null) return Enumerable.Empty<DeadLetterLogResponseDTO>();

            return deadLetterLogs.Select(ToDeadLetterLogResponseDTO).ToList();
        }

        public static DeadLetterLog ToEntity(string messageBody, System.Guid clientOrderId, string reason)
        {
            return new DeadLetterLog
            {
                ClientOrderId = clientOrderId,
                MessageBody = messageBody,
                Reason = reason
            };
        }
    }
}