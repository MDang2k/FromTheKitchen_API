using FromTheKitchen_API.Data;
using FromTheKitchen_API.Models;
using FromTheKitchen_API.Models.Dto;
using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FromTheKitchen_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderHeaderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ApiResponse _response;

        public OrderHeaderController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public ActionResult<ApiResponse> GetOrders(string userId = "")
        {
            IEnumerable<OrderHeader> orderHeaderList = _db.OrderHeaders.Include(u => u.OrderDetails)
                .ThenInclude(u => u.MenuItem).OrderByDescending(u => u.OrderHeaderId);

            // Customers only ever see their own orders, whatever userId they send
            if (!IsAdmin)
            {
                userId = CallerId;
            }

            if (!string.IsNullOrEmpty(userId))
            {
                orderHeaderList = orderHeaderList.Where(u => u.ApplicationUserId == userId);
            }

            _response.Result = orderHeaderList;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{orderId:int}")]
        public ActionResult<ApiResponse> GetOrder(int orderId)
        {
            if (orderId == 0)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Invalid order id");
                return BadRequest(_response);
            }

            OrderHeader? orderHeader = _db.OrderHeaders.Include(u => u.OrderDetails)
                .ThenInclude(u => u.MenuItem).FirstOrDefault(u => u.OrderHeaderId == orderId);

            if (orderHeader == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add("Order not found");
                return NotFound(_response);
            }

            if (!IsAdmin && orderHeader.ApplicationUserId != CallerId)
            {
                return Forbidden("You can't access this order");
            }

            _response.Result = orderHeader;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        public ActionResult<ApiResponse> CreateOrder([FromBody] OrderHeaderCreateDTO orderHeaderDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    OrderHeader orderHeader = new()
                    {
                        PickUpName = orderHeaderDTO.PickUpName,
                        PhoneNumber = orderHeaderDTO.PhoneNumber,
                        PickUpEmail = orderHeaderDTO.PickUpEmail,
                        OrderDate = DateTime.Now,
                        OrderTotal = orderHeaderDTO.OrderTotal,
                        Status = SD.status_confirmed,
                        TotalItem = orderHeaderDTO.TotalItem,
                        // Taken from the token, not the body, so nobody can place orders for someone else
                        ApplicationUserId = CallerId,

                    };

                    _db.OrderHeaders.Add(orderHeader);
                    _db.SaveChanges();


                    foreach (var orderDetailDTO in orderHeaderDTO.OrderDetailsDTO)
                    {
                        OrderDetail orderDetail = new()
                        {
                            OrderHeaderId = orderHeader.OrderHeaderId,
                            MenuItemId = orderDetailDTO.MenuItemId,
                            Quantity = orderDetailDTO.Quantity,
                            ItemName = orderDetailDTO.ItemName,
                            Price = orderDetailDTO.Price
                        };
                        _db.OrderDetails.Add(orderDetail);
                    }
                    _db.SaveChanges();
                    _response.Result = orderHeader;
                    orderHeader.OrderDetails = [];
                    _response.StatusCode = HttpStatusCode.Created;
                    return CreatedAtAction(nameof(GetOrder), new { orderId = orderHeader.OrderHeaderId }, _response);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = ModelState.Values.SelectMany(u => u.Errors).Select(u => u.ErrorMessage).ToList();
                    return BadRequest(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("{orderId:int}")]
        public ActionResult<ApiResponse> UpdateOrder(int orderId, [FromBody] OrderHeaderUpdateDTO orderHeaderDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (orderId!= orderHeaderDTO.OrderHeaderId)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.ErrorMessages.Add("Order id mismatch");
                        return BadRequest(_response);
                    }

                    OrderHeader orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.OrderHeaderId == orderId);

                    if (orderHeaderFromDb == null)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.NotFound;
                        _response.ErrorMessages.Add("Order not found");
                        return NotFound(_response);
                    }

                    if (!IsAdmin && orderHeaderFromDb.ApplicationUserId != CallerId)
                    {
                        return Forbidden("You can't access this order");
                    }

                    // Customers may cancel their own order; every other status change is for admins
                    if (!IsAdmin && !string.IsNullOrEmpty(orderHeaderDTO.Status)
                        && !IsStatus(orderHeaderDTO.Status, orderHeaderFromDb.Status)
                        && !IsStatus(orderHeaderDTO.Status, SD.status_cancelled))
                    {
                        return Forbidden("Only admins can change order status");
                    }

                    if (!string.IsNullOrEmpty(orderHeaderDTO.PickUpName))
                    {
                        orderHeaderFromDb.PickUpName = orderHeaderDTO.PickUpName;
                    }

                    if (!string.IsNullOrEmpty(orderHeaderDTO.PhoneNumber))
                    {
                        orderHeaderFromDb.PhoneNumber = orderHeaderDTO.PhoneNumber;
                    }

                    if (!string.IsNullOrEmpty(orderHeaderDTO.PickUpEmail))
                    {
                        orderHeaderFromDb.PickUpEmail = orderHeaderDTO.PickUpEmail;
                    }

                    // Sending the current status again is allowed and changes nothing
                    string current = orderHeaderFromDb.Status;
                    string requested = orderHeaderDTO.Status;
                    if (!string.IsNullOrEmpty(requested) && !IsStatus(requested, current))
                    {
                        string? next = null;
                        if (IsStatus(current, SD.status_confirmed) && IsStatus(requested, SD.status_readyForPickUp))
                        {
                            next = SD.status_readyForPickUp;
                        }
                        else if (IsStatus(current, SD.status_readyForPickUp) && IsStatus(requested, SD.status_completed))
                        {
                            next = SD.status_completed;
                        }
                        else if (IsStatus(requested, SD.status_cancelled) && !IsStatus(current, SD.status_completed))
                        {
                            next = SD.status_cancelled;
                        }

                        if (next == null)
                        {
                            _response.IsSuccess = false;
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.ErrorMessages.Add(IsStatus(requested, SD.status_cancelled)
                                ? "Completed orders can't be cancelled"
                                : $"Can't change order status from '{current}' to '{requested}'");
                            return BadRequest(_response);
                        }

                        orderHeaderFromDb.Status = next;
                    }

                    _db.SaveChanges();


                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = ModelState.Values.SelectMany(u => u.Errors).Select(u => u.ErrorMessage).ToList();
                    return BadRequest(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        private static bool IsStatus(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);

        // "id" is the user id claim AuthController puts in the JWT
        private string CallerId => User.FindFirst("id")?.Value ?? "";
        private bool IsAdmin => User.IsInRole(SD.Role_Admin);

        private ObjectResult Forbidden(string message)
        {
            _response.IsSuccess = false;
            _response.StatusCode = HttpStatusCode.Forbidden;
            _response.ErrorMessages.Add(message);
            return StatusCode((int)HttpStatusCode.Forbidden, _response);
        }
    }
}
