using FromTheKitchen_API.Models;
using FromTheKitchen_API.Models.Dto;
using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FromTheKitchen_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {

        private readonly ApiResponse _response;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string secretKey;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _response = new ApiResponse();
            secretKey = configuration.GetValue<string>("ApiSettings:Secret")??"";
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
        {
            if (ModelState.IsValid)
            {
                // Only an Admin can create another Admin. The first Admin is seeded from configuration (AdminSeeder).
                bool wantsAdmin = model.Role.Equals(SD.Role_Admin, StringComparison.OrdinalIgnoreCase);
                if (wantsAdmin && !User.IsInRole(SD.Role_Admin))
                {
                    _response.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Only admins can create admin accounts");
                    return StatusCode((int)System.Net.HttpStatusCode.Forbidden, _response);
                }

                ApplicationUser newUser = new()
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    NormalizedEmail = model.Email.ToUpper(),
                };

                var result = _userManager.CreateAsync(newUser, model.Password).Result;
                if (result.Succeeded)
                {
                    if (wantsAdmin)
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Admin);

                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Customer);
                    }

                        _response.StatusCode = System.Net.HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        _response.ErrorMessages.Add(error.Description);
                    }
                    _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest(_response);
                }
            }
            else
            {
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.IsSuccess = false;

                foreach (var error in ModelState.Values)
                {
                    foreach (var subError in error.Errors)
                    {
                        _response.ErrorMessages.Add(subError.ErrorMessage);
                    }
                }
                return BadRequest(_response);
            }
            
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            if (ModelState.IsValid)
            {

                var userFromDb = await _userManager.FindByEmailAsync(model.Email);

                if (userFromDb != null)
                {
                    bool isValid = await _userManager.CheckPasswordAsync(userFromDb, model.Password);
                    if (!isValid)
                    {
                        _response.Result = new LoginResponseDTO();
                        _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages.Add("Invalid Credentials");
                        return BadRequest(_response);
                    }

                    // The token carries one role. A user promoted to Admin keeps Customer too, and the
                    // role list has no fixed order, so pick Admin explicitly.
                    var roles = await _userManager.GetRolesAsync(userFromDb);
                    string role = roles.Contains(SD.Role_Admin) ? SD.Role_Admin : roles.FirstOrDefault()!;

                    JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                    byte[] key = Encoding.ASCII.GetBytes(secretKey);

                    SecurityTokenDescriptor tokenDescriptor = new()
                    {
                        Subject = new System.Security.Claims.ClaimsIdentity([
                            new ("fullname", userFromDb.Id),
                            new ("id", userFromDb.Id),
                            new (ClaimTypes.Email, userFromDb.Email!.ToString()),
                            new (ClaimTypes.Role, role)
                            ]),
                        Expires = DateTime.UtcNow.AddDays(7),
                        SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

                    LoginResponseDTO loginResponse = new()
                    {
                        Email = userFromDb.Email,
                        Token = tokenHandler.WriteToken(token),
                        Role = role
                    };

                    _response.Result = loginResponse;
                    _response.StatusCode = System.Net.HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }

                _response.Result = new LoginResponseDTO();
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid Credentials");
                return BadRequest(_response);

            }
            else
            {
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.IsSuccess = false;

                foreach (var error in ModelState.Values)
                {
                    foreach (var subError in error.Errors)
                    {
                        _response.ErrorMessages.Add(subError.ErrorMessage);
                    }
                }
                return BadRequest(_response);
            }

        }

    }
}
