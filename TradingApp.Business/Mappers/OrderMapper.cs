using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TradingApp.Business.DTOs;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Mappers
{
    public static class OrderMapper
    {
        public static CreateOrderRequestDTO ToCreateOrderRequestDTO(Order entity)
        {
            if (entity == null) return null;

            return new CreateOrderRequestDTO
            {
                Status = entity.Status,
                Quantity = entity.Quantity,
                Price = entity.Price,
            };
        }

        public static CreateOrderResponseDTO ToCreatedOrderResponseDTO(Order entity) 
        {
            if (entity == null) return null;

            return new CreateOrderResponseDTO
            {
                Id = entity.Id,
                ClientOrderId = entity.ClientOrderId,
                Status = entity.Status,
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
                Status = dto.Status,
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
                Status = entity.Status,
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
