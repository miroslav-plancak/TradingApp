using TradingApp.Business.DTOs;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Mappers
{
    public static class OrderMapper
    {
        static OrderMapper(){}

        public static CreateOrderRequestDTO ToDTO(Order entity)
        {
            if (entity == null) return null;

            return new CreateOrderRequestDTO
            {
                Status = entity.Status,
                Quantity = entity.Quantity,
                Price = entity.Price,
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
    }
}
