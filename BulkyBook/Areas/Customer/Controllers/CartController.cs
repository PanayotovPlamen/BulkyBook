﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BulkyBook.Web.Areas.Customer.Controllers
{
	[Area("Customer")]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		private readonly UserManager<IdentityUser> _userManager;

		public ShoppingCartViewModel ShoppingCartViewModel { get; set; }

		public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, UserManager<IdentityUser> userManager)
		{
			_unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_userManager = userManager;
		}

        public IActionResult Index()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;

            Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartViewModel = new ShoppingCartViewModel()
            {
                OrderHeader = new OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")
            };

            ShoppingCartViewModel.OrderHeader.OrderTotal = 0;

            ShoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                                                        .GetFirstOrDefault(u => u.Id == claim.Value,
                                                        includeProperties: "Company");

            foreach (ShoppingCart list in ShoppingCartViewModel.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price,
                                                    list.Product.Price50, list.Product.Price100);

                ShoppingCartViewModel.OrderHeader.OrderTotal += (list.Price * list.Count);

                list.Product.Description = SD.ConvertToRawHtml(list.Product.Description);

                if (list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }

            return View(ShoppingCartViewModel);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;

            Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ApplicationUser user = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Verification email is empty!");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");

            return RedirectToAction("Index");

        }

        public IActionResult Plus(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");

            cart.Count += 1;

            cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                                    cart.Product.Price50, cart.Product.Price100);

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");

            if (cart.Count == 1)
            {
                int cnt = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

                _unitOfWork.ShoppingCart.Remove(cart);

                _unitOfWork.Save();

                HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
            }
            else
            {
                cart.Count -= 1;

                cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                                    cart.Product.Price50, cart.Product.Price100);

                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");

            int cnt = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

            _unitOfWork.ShoppingCart.Remove(cart);

            _unitOfWork.Save();

            HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);

            return RedirectToAction(nameof(Index));
        }
    }
}
