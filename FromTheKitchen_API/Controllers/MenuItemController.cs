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
    [Route("api/MenuItem")]
    [ApiController]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ApiResponse _response;
        private readonly string _imagesFolder;
        public MenuItemController(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _imagesFolder = SD.ImagesFolder(config, env);
            _response = new ApiResponse();
        }

        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
        private const long MaxImageBytes = 5 * 1024 * 1024;

        // Images are served from the API's own origin, so only real image types get in (no .html, .svg or .js).
        private static string? ImageError(IFormFile file) =>
            !AllowedImageExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()) ? "The image must be a JPG, PNG, WebP or GIF file."
            : file.Length > MaxImageBytes ? "The image must be 5 MB or smaller."
            : null;

        private BadRequestObjectResult Invalid(params string[] errors)
        {
            _response.IsSuccess = false;
            _response.StatusCode = HttpStatusCode.BadRequest;
            _response.ErrorMessages = errors.Length > 0
                ? [.. errors]
                : ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(_response);
        }

        // Saves the upload to the images folder and returns the value stored in MenuItem.Image
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            // A fresh name every time: an upload never overwrites another item's image,
            // and the client's file name (e.g. "../../x.jpg") is never used as a path.
            string fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            Directory.CreateDirectory(_imagesFolder);
            using (var stream = new FileStream(Path.Combine(_imagesFolder, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "images/" + fileName;
        }

        // ponytail: with ImageStorage:Path set, seeded images (in wwwroot) are left alone; every deploy ships them anyway
        private void DeleteImage(string image)
        {
            var path = Path.Combine(_imagesFolder, Path.GetFileName(image));
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        [HttpGet]
        public IActionResult GetMenuItems()
        {
            _response.Result = _context.MenuItems;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{id:int}", Name = "GetMenuItem")]
        public IActionResult GetMenuItem(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }

            MenuItem? menuItem = _context.MenuItems.FirstOrDefault(u => u.Id == id);
            if (menuItem == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Menu item not found");
                return NotFound(_response);
            }

            _response.Result = menuItem;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm] MenuItemCreateDTO menuItemCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemCreateDTO.File == null || menuItemCreateDTO.File.Length == 0)
                    {
                        return Invalid("No file uploaded.");
                    }
                    if (ImageError(menuItemCreateDTO.File) is string imageError)
                    {
                        return Invalid(imageError);
                    }

                    MenuItem menuItem = new()
                    {
                        Name = menuItemCreateDTO.Name,
                        Description = menuItemCreateDTO.Description,
                        Category = menuItemCreateDTO.Category,
                        SpecialTag = menuItemCreateDTO.SpecialTag,
                        Price = menuItemCreateDTO.Price,
                        Image = await SaveImageAsync(menuItemCreateDTO.File)
                    };

                    _context.MenuItems.Add(menuItem);
                    await _context.SaveChangesAsync();

                    _response.Result = menuItem;
                    _response.StatusCode = HttpStatusCode.Created;
                    return CreatedAtRoute("GetMenuItem", new { id = menuItem.Id }, _response);

                }
                else
                {
                    return Invalid();
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = [ex.ToString()];
            }

            return BadRequest(_response);
        }

        [HttpPut]
        [Authorize(Roles = SD.Role_Admin)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDTO menuItemUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDTO.Id != id)
                    {
                        return Invalid("Menu item id mismatch.");
                    }

                    MenuItem? menuItemFromDb = await _context.MenuItems.FirstOrDefaultAsync(u => u.Id == id);

                    if (menuItemFromDb == null)
                    {
                        return Invalid("Menu item not found.");
                    }

                    // A new image is optional: editing the name or price keeps the current one.
                    bool newImage = menuItemUpdateDTO.File is { Length: > 0 };
                    if (newImage && ImageError(menuItemUpdateDTO.File!) is string imageError)
                    {
                        return Invalid(imageError);
                    }

                    menuItemFromDb.Name = menuItemUpdateDTO.Name;
                    menuItemFromDb.Description = menuItemUpdateDTO.Description;
                    menuItemFromDb.Category = menuItemUpdateDTO.Category;
                    menuItemFromDb.SpecialTag = menuItemUpdateDTO.SpecialTag;
                    menuItemFromDb.Price = menuItemUpdateDTO.Price;

                    string oldImage = menuItemFromDb.Image;
                    if (newImage)
                    {
                        menuItemFromDb.Image = await SaveImageAsync(menuItemUpdateDTO.File!);
                    }

                    _context.MenuItems.Update(menuItemFromDb);
                    await _context.SaveChangesAsync();

                    // Only after the row points at the new file, so a failed save never loses the image.
                    if (newImage)
                    {
                        DeleteImage(oldImage);
                    }

                    _response.Result = menuItemFromDb;
                    return Ok(_response);

                }
                else
                {
                    return Invalid();
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = [ex.ToString()];
            }

            return BadRequest(_response);
        }

        [HttpDelete]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (id == 0)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }

                    MenuItem? menuItemFromDb = await _context.MenuItems.FirstOrDefaultAsync(u => u.Id == id);

                    if (menuItemFromDb == null)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.NotFound;
                        return NotFound(_response);
                    }

                    // The OrderDetails foreign key cascades, so deleting an ordered item would erase lines from past orders.
                    // ponytail: refuse instead; make OrderDetail.MenuItemId nullable (SetNull) or add an "available" flag if retiring dishes matters.
                    if (await _context.OrderDetails.AnyAsync(d => d.MenuItemId == id))
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.Conflict;
                        _response.ErrorMessages.Add("This item is in past orders, so it can't be deleted. Edit it instead.");
                        return Conflict(_response);
                    }

                    _context.MenuItems.Remove(menuItemFromDb);
                    await _context.SaveChangesAsync();
                    DeleteImage(menuItemFromDb.Image);

                    _response.Result = HttpStatusCode.NoContent;
                    return Ok(_response);

                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = [ex.ToString()];
            }

            return BadRequest(_response);
        }
    }
}
