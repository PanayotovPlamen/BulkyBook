﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderDetailsViewModel OrderViewModel { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;   
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            OrderViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id,
                                                includeProperties: "ApplicationUser"),

                OrderDetails = _unitOfWork.OrderDetails.GetAll(o => o.OrderId == id, includeProperties: "Product")

            };

            return View(OrderViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Details")]
        public IActionResult Details(string stripeToken)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderViewModel.OrderHeader.Id,
                                                includeProperties: "ApplicationUser");

            if (stripeToken != null)
            {
                //process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "Order ID : " + orderHeader.Id,
                    Source = stripeToken
                };

                ChargeService service = new ChargeService();

                Charge charge = service.Create(options);

                if (charge.Id == null)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    orderHeader.TransactionId = charge.Id;
                }

                if (charge.Status.ToLower() == "succeeded")
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;

                    orderHeader.PaymentDate = DateTime.Now;
                }

                _unitOfWork.Save();

            }

            return RedirectToAction("Details", "Order", new { id = orderHeader.Id });
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);

            orderHeader.OrderStatus = SD.StatusInProcess;

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderViewModel.OrderHeader.Id);

            orderHeader.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;

            orderHeader.Carrier = OrderViewModel.OrderHeader.Carrier;

            orderHeader.OrderStatus = SD.StatusShipped;

            orderHeader.ShippingDate = DateTime.Now;

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);

            if (orderHeader.PaymentStatus == SD.StatusApproved)
            {

                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = orderHeader.TransactionId

                };

                RefundService service = new RefundService();

                Refund refund = service.Create(options);

                orderHeader.OrderStatus = SD.StatusRefunded;

                orderHeader.PaymentStatus = SD.StatusRefunded;
            }
            else
            {
                orderHeader.OrderStatus = SD.StatusCancelled;

                orderHeader.PaymentStatus = SD.StatusCancelled;
            }

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        public IActionResult UpdateOrderDetails()
        {
            OrderHeader orderHEaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderViewModel.OrderHeader.Id);

            orderHEaderFromDb.Name = OrderViewModel.OrderHeader.Name;

            orderHEaderFromDb.PhoneNumber = OrderViewModel.OrderHeader.PhoneNumber;

            orderHEaderFromDb.StreetAddress = OrderViewModel.OrderHeader.StreetAddress;

            orderHEaderFromDb.City = OrderViewModel.OrderHeader.City;

            orderHEaderFromDb.State = OrderViewModel.OrderHeader.State;

            orderHEaderFromDb.PostalCode = OrderViewModel.OrderHeader.PostalCode;

            if (OrderViewModel.OrderHeader.Carrier != null)
            {
                orderHEaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
            }

            if (OrderViewModel.OrderHeader.TrackingNumber != null)
            {
                orderHEaderFromDb.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            }

            _unitOfWork.Save();

            TempData["Error"] = "Order Details Updated Successfully.";

            return RedirectToAction("Details", "Order", new { id = orderHEaderFromDb.Id });
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetOrderList(string status)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeaderList;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(
                                        u => u.ApplicationUserId == claim.Value,
                                        includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaderList = orderHeaderList.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusApproved ||
                                                            o.OrderStatus == SD.StatusInProcess ||
                                                            o.OrderStatus == SD.StatusPending);
                    break;
                case "completed":
                    orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusShipped);
                    break;
                case "rejected":
                    orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusCancelled ||
                                                            o.OrderStatus == SD.StatusRefunded ||
                                                            o.OrderStatus == SD.PaymentStatusRejected);
                    break;
                default:
                    break;
            }

            return Json(new { data = orderHeaderList });
        }

        #endregion

    }
}
