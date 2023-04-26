using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        public void Update(OrderHeader obj);
        public void UpdateStatus(int id, string orderStatus, string paymentStatus = null);
    }
}
