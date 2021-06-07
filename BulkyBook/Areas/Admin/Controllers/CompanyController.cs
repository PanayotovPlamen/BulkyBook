using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class CompanyController : Controller
	{

        private readonly IUnitOfWork _unitOfWork;

		public CompanyController(IUnitOfWork unitOfWork)
		{
            _unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}

        public IActionResult Upsert(int? id)
        {
            Company company = new Company();

            if (id == null)
            {
                //Create
                return View(company);
            }

            //Edit
            company = _unitOfWork.Company.Get(id.GetValueOrDefault());

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    _unitOfWork.Company.Add(company);
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                }

                _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }

            return View(company);
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var companies = _unitOfWork.Company.GetAll();

            return Json(new { data = companies });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            Company company = _unitOfWork.Company.Get(id);

            if (company == null)
            {
                return Json(new { success = false, message = "An error occured during the process." });
            }

            _unitOfWork.Company.Remove(company);

            _unitOfWork.Save();

            return Json(new { success = true, message = "The company was deleted successfully." });

        }
        #endregion
    }
}
