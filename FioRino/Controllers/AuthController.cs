using Microsoft.Extensions.Configuration;
using FioRino.Entities.DTOs;
using FioRino.Entities.Models;
using FioRino.Responces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FioRino.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterDTO dTO)
        {
            var userExists = await _userManager.FindByEmailAsync(dTO.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response
                { Message = "User already exists! Duplication is prohibited!", Status = "Error" });
            User user = new User
            {
                FirstName = dTO.FirstName,
                LastName = dTO.LastName,
                MiddleName = dTO.MiddleName,
                UserName = dTO.Email,
                Email = dTO.Email,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await _userManager.CreateAsync(user, dTO.Password);
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, "User");
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response
                { Message = "User is not registered!", Status = "Error" });
            return Ok(new Response { Message = "User is registered successfully!", Status = "Success" });
        }
        [HttpPost]
        [Route("Login")]

        public async Task<IActionResult> Login([FromBody] LoginDTO dTO)
        {
            var userExists = await _userManager.FindByEmailAsync(dTO.Email);
            if (userExists != null && await _userManager.CheckPasswordAsync(userExists, dTO.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(userExists);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userExists.Email),
                    new Claim(ClaimTypes.NameIdentifier, userExists.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };
                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }
                var authSigninKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidateAudience"],
                    expires: DateTime.Now.AddHours(8),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigninKey, SecurityAlgorithms.HmacSha256));
                var tokenJWT = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(new
                {
                    Status = "Success",
                    token = "Bearer " + tokenJWT,

                    Expiration = token.ValidTo
                });
            }
            return BadRequest(new
            {
                Status = "Bad request!",
                Error = "User is not registered!"
            });
        }
        [HttpGet]
        [Route("GetUser")]
        public async Task<IActionResult> GetUser()
        {
            var claim = this.User.Identity as ClaimsIdentity;
            var currentUser = await _userManager.GetUserAsync(User);
            var user = User.Identity.IsAuthenticated;
            var userInfo = new UserDTO
            {
                Id = currentUser.Id,
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                MiddleName = currentUser.MiddleName,
                Email = currentUser.Email,
                Password = currentUser.PasswordHash
            };
            if (userInfo == null)
                return BadRequest(new Response { Message = "User is not authenticated!", Status = "Error" });
            return Ok(new
            {
                User = userInfo,
                IsAuthenticated = user
            });
        }
    }
}
