using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _config = config;
            _repo = repo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegister)
        {
            userForRegister.Username = userForRegister.Username.ToLower();
            if (await _repo.UserExists(userForRegister.Username))
                return BadRequest("User already exists.");
            User userToCreate = new User
            {
                Username = userForRegister.Username
            };
            var userCreated = await _repo.Register(userToCreate, userForRegister.Password);
            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);
            if (userFromRepo == null)
                return Unauthorized();

            // Create Claim for our token
            var claims = new[]{
                new Claim(ClaimTypes.NameIdentifier,userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name,userFromRepo.Username)
            };

            // Creating a security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            // key is using with signing crediential and encrypting it with hashing algorithm
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // Creating token and passing claims and other information.
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            // With the help of token handler we are creating toking by passing token descriptor.
            var tokenHandler = new JwtSecurityTokenHandler();
            var token =  tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new{
                token = tokenHandler.WriteToken(token)
            });

        }
    }
}