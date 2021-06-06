using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
	public class ProductRepository : Repository<Product>, IProductRepository
	{

		private readonly ApplicationDbContext _context;

		public ProductRepository(ApplicationDbContext context)  : base(context)
		{
			_context = context;
		}

		public void Update(Product product)
		{
			Product objFromDb = _context.Products.FirstOrDefault(s => s.Id == product.Id);

			if (objFromDb != null)
			{
				objFromDb.Title = product.Title;

				objFromDb.Description = product.Description;

				objFromDb.ISBN = product.ISBN;

				objFromDb.Author = product.Author;

				objFromDb.ListPrice = product.ListPrice;

				objFromDb.Price = product.Price;

				objFromDb.Price50 = product.Price50;

				objFromDb.Price100 = product.Price100;

				objFromDb.ImageUrl = product.ImageUrl;

				objFromDb.CategoryId = product.CategoryId;

				objFromDb.CoverTypeId = product.CoverTypeId;

				_context.SaveChanges();
			}
		}
	}
}
