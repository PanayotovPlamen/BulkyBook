using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
	public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public CoverTypeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(CoverType coverType)
        {
            var objFromDb = _context.CoverTypes.FirstOrDefault(s => s.Id == coverType.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = coverType.Name;

                _context.SaveChanges();
            }

        }

    }
}
