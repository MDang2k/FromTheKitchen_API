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

        // Saves the upload to the images folder and returns the value stored in MenuItem.Image
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            // Only the file name: a client-sent "../../x.jpg" must not write outside the folder
            string fileName = Path.GetFileName(file.FileName);
            Directory.CreateDirectory(_imagesFolder);
            using (var stream = new FileStream(Path.Combine(_imagesFolder, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "images/" + fileName;
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
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.ErrorMessages = ["No file uploaded."];
                        return BadRequest(_response);
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

                    _response.Result = menuItemCreateDTO;
                    _response.StatusCode = HttpStatusCode.Created;
                    return CreatedAtRoute("GetMenuItem", new { id = menuItem.Id }, _response);

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

        [HttpPut]
        [Authorize(Roles = SD.Role_Admin)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDTO menuItemUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDTO.File == null || menuItemUpdateDTO.Id != id)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }

                    MenuItem? menuItemFromDb = await _context.MenuItems.FirstOrDefaultAsync(u => u.Id == id);

                    if (menuItemFromDb == null)
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }

                    menuItemFromDb.Name = menuItemUpdateDTO.Name;
                    menuItemFromDb.Description = menuItemUpdateDTO.Description;
                    menuItemFromDb.Category = menuItemUpdateDTO.Category;
                    menuItemFromDb.SpecialTag = menuItemUpdateDTO.SpecialTag;
                    menuItemFromDb.Price = menuItemUpdateDTO.Price;

                    if (menuItemUpdateDTO.File != null && menuItemUpdateDTO.File.Length > 0)
                    {
                        menuItemFromDb.Image = await SaveImageAsync(menuItemUpdateDTO.File);
                    }


                    _context.MenuItems.Update(menuItemFromDb);
                    await _context.SaveChangesAsync();

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

                    // ponytail: with ImageStorage:Path set, seeded images (in wwwroot) are left alone; every deploy ships them anyway
                    var filePath_old = Path.Combine(_imagesFolder, Path.GetFileName(menuItemFromDb.Image));
                    if (System.IO.File.Exists(filePath_old))
                    {
                        System.IO.File.Delete(filePath_old);
                    }


                    _context.MenuItems.Remove(menuItemFromDb);
                    await _context.SaveChangesAsync();

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
