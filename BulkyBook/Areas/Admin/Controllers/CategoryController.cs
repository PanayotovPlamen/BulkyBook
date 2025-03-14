﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Category category = new Category();

            if (id == null)
            {
                //Create
                return View(category);
            }

            //Edit
            category = _unitOfWork.Category.Get(id.GetValueOrDefault());

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Category category)
        {
            if (ModelState.IsValid)
            {
                if (category.Id == 0)
                {
                    _unitOfWork.Category.Add(category);
                }
                else
                {
                    _unitOfWork.Category.Update(category);
                }

                _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
           var categories = _unitOfWork.Category.GetAll();

            return Json(new { data = categories });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            Category category = _unitOfWork.Category.Get(id);

            if (category == null)
            {
                return Json(new { success = false, message = "An error occured during the process." });
            }

            _unitOfWork.Category.Remove(category);

            _unitOfWork.Save();

            return Json(new { success = true, message = "The category was deleted successfully." });
            
        }

        #endregion

    }
}
