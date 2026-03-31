using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Day1Vizov.Data;
using Day1Vizov.Models;
using Day1Vizov.DTOs;
using Day1Vizov.Services;
using Serilog;

namespace Day1Vizov.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    
    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }
    
    //регистрация
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        Log.Information("POST /api/auth/register - попытка регистрации: {Username} с ролью {Role}", 
            registerDto.Username, registerDto.Role);
        
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == registerDto.Username);
        
        if (existingUser != null)
        {
            Log.Warning("POST /api/auth/register - пользователь уже существует: {Username}", registerDto.Username);
            return BadRequest(new { error = "Пользователь уже существует" });
        }
        
        var validRoles = new[] { "User", "Admin" };
        if (!validRoles.Contains(registerDto.Role))
        {
            Log.Warning("POST /api/auth/register - некорректная роль: {Role}", registerDto.Role);
            return BadRequest(new { error = "Роль должна быть User или Admin" });
        }
        
        var passwordHash = HashPassword(registerDto.Password);
        
        var user = new User
        {
            Username = registerDto.Username,
            PasswordHash = passwordHash,
            Role = registerDto.Role
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        Log.Information("POST /api/auth/register - пользователь зарегистрирован: {Username} (ID: {UserId}, Роль: {Role})", 
            user.Username, user.Id, user.Role);
        
        return Ok(new { message = $"Регистрация успешна. Ваша роль: {user.Role}" });
    }
    
    //вход
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        Log.Information("POST /api/auth/login - попытка входа: {Username}", loginDto.Username);
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username);
        
        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            Log.Warning("POST /api/auth/login - неверные учетные данные: {Username}", loginDto.Username);
            return Unauthorized(new { error = "Неверное имя пользователя или пароль" });
        }
        
        var token = _tokenService.GenerateToken(user);
        
        Log.Information("POST /api/auth/login - пользователь вошел: {Username} (ID: {UserId}, Роль: {Role})", 
            user.Username, user.Id, user.Role);
        
        var response = new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        
        return Ok(response);
    }
    
    //хешируем парольчик
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
    
    //проверяем парольчик
    private bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }
}