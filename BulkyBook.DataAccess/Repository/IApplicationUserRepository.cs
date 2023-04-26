using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public interface IApplicationUserRepository : IRepository<ApplicationUser>
    {
        public void Update(ApplicationUser obj);
    }
}
