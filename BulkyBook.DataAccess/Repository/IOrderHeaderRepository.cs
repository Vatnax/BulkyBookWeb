using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public interface IOrderDetailRepository : IRepository<OrderDetail>
    {
        public void Update(OrderDetail obj);
    }
}
