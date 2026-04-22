using System.Collections.Generic;
using System.Linq;
using TradingApp.Business.DTOs;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Mappers
{
    public static class OrderMapper
    {
        public static CreatedOrderResponseDTO ToCreatedOrderResponseDTO(Order entity) 
        {
            if (entity == null) return null;

            return new CreatedOrderResponseDTO
            {
                Id = entity.Id,
                ClientOrderId = entity.ClientOrderId,
                Status = entity.Status.ToString(),
                Quantity = entity.Quantity,
                Price = entity.Price,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static Order  ToEntity(CreateOrderRequestDTO dto)
        {
            if (dto == null) return null;

            return new Order
            {
                Quantity = dto.Quantity,
                Price = dto.Price,
            };
        }

        public static OrderResponseDTO ToOrderResponseDTO(Order entity)
        {
            if (entity == null) return null;

            return new OrderResponseDTO
            {
                Id = entity.Id,
                ClientOrderId = entity.ClientOrderId,
                Status = entity.Status.ToString(),
                Quantity = entity.Quantity,
                Price = entity.Price,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static IEnumerable<OrderResponseDTO> ToOrderResponseDTOs(IEnumerable<Order> entities)
        {
            return entities?.Select(ToOrderResponseDTO).ToList() ?? new List<OrderResponseDTO>();
        }
    }
}
