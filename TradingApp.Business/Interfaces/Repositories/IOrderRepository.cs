using System.Threading.Tasks;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        Task CreateOrderAsync(Order order);
        Task<Order> GetOrdersAsync(Order order);
        Task<Order> GetOrderByIdAsync(Order order);
    }
}
