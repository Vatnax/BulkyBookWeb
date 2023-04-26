using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public interface ICoverTypeRepository : IRepository<CoverType>
    {
        public void Update(CoverType obj);
    }
}
