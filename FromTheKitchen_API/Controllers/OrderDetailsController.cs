using FromTheKitchen_API.Data;
using FromTheKitchen_API.Models;
using FromTheKitchen_API.Models.Dto;
using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FromTheKitchen_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderDetailsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ApiResponse _response;

        public OrderDetailsController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpPut("{orderDetailsId:int}")]
        public ActionResult<ApiResponse> UpdateOrder(int orderDetailsId, [FromBody] OrderDetailsUpdateDTO orderDetailsDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (orderDetailsId != orderDetailsDTO.OrderDetailId)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.ErrorMessages.Add("Order id mismatch");
                        return BadRequest(_response);
                    }

                    OrderDetail? orderDetailsFromDb = _db.OrderDetails.FirstOrDefault(u => u.OrderDetailId == orderDetailsId);

                    if (orderDetailsFromDb == null)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.NotFound;
                        _response.ErrorMessages.Add("Order not found");
                        return NotFound(_response);
                    }

                    // Only the customer who placed the order can rate its items
                    string? ownerId = _db.OrderHeaders.Where(u => u.OrderHeaderId == orderDetailsFromDb.OrderHeaderId)
                        .Select(u => u.ApplicationUserId).FirstOrDefault();
                    if (ownerId != User.FindFirst("id")?.Value)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.Forbidden;
                        _response.ErrorMessages.Add("You can only rate items from your own orders");
                        return StatusCode((int)HttpStatusCode.Forbidden, _response);
                    }

                    orderDetailsFromDb.Rating = orderDetailsDTO.Rating;

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
    }
}
