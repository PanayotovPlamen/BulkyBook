
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
	public class CompanyRepository : Repository<Company>, ICompanyRepository
	{
		private readonly ApplicationDbContext _context;

		public CompanyRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}

		public void Update(Company company)
		{
			Company objFromDb = _context.Companies.FirstOrDefault(s => s.Id == company.Id);

			if (objFromDb != null)
			{
				objFromDb.Name = company.Name;

				objFromDb.StreetAddress = company.StreetAddress;

				objFromDb.City = company.City;

				objFromDb.State = company.State;

				objFromDb.PostalCode = company.PostalCode;

				objFromDb.PhoneNumber = company.PhoneNumber;

				objFromDb.IsAuthorizedCompany = company.IsAuthorizedCompany;

				_context.SaveChanges();
			}

		}
	}
}
