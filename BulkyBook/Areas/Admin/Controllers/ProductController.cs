using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class ProductController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		private readonly IWebHostEnvironment _hostEnvironment;

		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
		{
			_unitOfWork = unitOfWork;

            _hostEnvironment = hostEnvironment;
		}

		public IActionResult Index()
		{
			return View();
		}

        public IActionResult Upsert(int? id)
        {

            ProductViewModel viewModel = new ProductViewModel()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            if (id == null)
            {
                //Create
                return View(viewModel);
            }

            //Edit
            viewModel.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());

            if (viewModel.Product == null)
            {
                return NotFound();
            }

            return View(viewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;

                var files = HttpContext.Request.Form.Files;

                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();

                    string uploads = Path.Combine(webRootPath, @"images\products");

                    string extenstion = Path.GetExtension(files[0].FileName);

                    if (viewModel.Product.ImageUrl != null)
                    {
                        //On edit we need to remove old image
                        string imagePath = Path.Combine(webRootPath, viewModel.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    using (FileStream filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }

                    viewModel.Product.ImageUrl = @"\images\products\" + fileName + extenstion;
                }
                else
                {
                    //Update when the user do not change the image
                    if (viewModel.Product.Id != 0)
                    {
                        Product objFromDb = _unitOfWork.Product.Get(viewModel.Product.Id);

                        viewModel.Product.ImageUrl = objFromDb.ImageUrl;
                    }
                }


                if (viewModel.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(viewModel.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(viewModel.Product);
                }

                _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            else
            {
                viewModel.CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });

                viewModel.CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });

                if (viewModel.Product.Id != 0)
                {
                    viewModel.Product = _unitOfWork.Product.Get(viewModel.Product.Id);
                }
            }

            return View(viewModel);
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var allObj = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");

            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            Product objFromDb = _unitOfWork.Product.Get(id);

            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string webRootPath = _hostEnvironment.WebRootPath;

            string imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            _unitOfWork.Product.Remove(objFromDb);

            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
