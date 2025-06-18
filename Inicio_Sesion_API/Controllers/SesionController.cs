using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Inicio_Sesion_API.Data;
using Inicio_Sesion_API.Models;
using System;

namespace Inicio_Sesion_API.Controllers;

[ApiController]
[Route("sesiones")]
public class SesionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public SesionController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }


    // Nuevo endpoint para validar si un token es válido o no
    [HttpGet("validar-token")]
    public async Task<IActionResult> ValidarToken([FromHeader(Name = "Authorization")] string token)
    {
        if (await ValidateAdminTokenAsync(token))
        {
            return Ok(new { mensaje = "Token válido" });
        }
        else
        {
            return Unauthorized(new { mensaje = "Token inválido o expirado" });
        }
    }

        private bool ValidateTokenFormat(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ValidateAdminTokenAsync(string token)
    {
        if (!ValidateTokenFormat(token))
            return false;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring(7);

        var tokenEntry = await _context.Token.FirstOrDefaultAsync(t => t.token == token && t.administrador_id != null);
        return tokenEntry != null;
    }

}
