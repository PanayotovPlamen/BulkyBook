using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
	public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(Category category)
        {
            var objFromDb = _context.Categories.FirstOrDefault(s => s.Id == category.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = category.Name;

                _context.SaveChanges();
            }

        }
    }
}
