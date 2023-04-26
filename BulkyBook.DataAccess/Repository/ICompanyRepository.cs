using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public interface ICompanyRepository : IRepository<Company>
    {
        public void Update(Company obj);
    }
}
