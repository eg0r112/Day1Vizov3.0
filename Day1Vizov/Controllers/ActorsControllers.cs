using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Day1Vizov.Data;
using Day1Vizov.Models;
using Day1Vizov.DTOs;
using Serilog;

namespace Day1Vizov.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] //чтоб все методы требовали авторизации пишем тута
public class ActorsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public ActorsController(AppDbContext context)
    {
        _context = context;
    }
    
    //список всех актёров - доступно всем кто авторизован!
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        Log.Information("GET /api/actors - запрос всех актёров от пользователя {User}", 
            User.Identity?.Name ?? "unknown");
        
        var actors = await _context.Actors
            .Include(a => a.MovieActors)
            .ThenInclude(ma => ma.Movie)
            .ToListAsync();
        
        var result = actors.Select(a => new ActorDto
        {
            Id = a.Id,
            Name = a.Name,
            BirthYear = a.BirthYear,
            MoviesCount = a.MovieActors.Count,
            Movies = a.MovieActors.Select(ma => new MovieSimpleDto
            {
                Id = ma.Movie.Id,
                Title = ma.Movie.Title,
                Year = ma.Movie.Year
            }).ToList()
        });
        
        return Ok(result);
    }
    
    //инфа о конкретном актёре - доступно всем кто авторизован!
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Log.Information("GET /api/actors/{Id} - запрос актёра от пользователя {User}", 
            id, User.Identity?.Name ?? "unknown");
        
        var actor = await _context.Actors
            .Include(a => a.MovieActors)
            .ThenInclude(ma => ma.Movie)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (actor == null)
            return NotFound();
        
        var result = new ActorDto
        {
            Id = actor.Id,
            Name = actor.Name,
            BirthYear = actor.BirthYear,
            MoviesCount = actor.MovieActors.Count,
            Movies = actor.MovieActors.Select(ma => new MovieSimpleDto
            {
                Id = ma.Movie.Id,
                Title = ma.Movie.Title,
                Year = ma.Movie.Year
            }).ToList()
        };
        
        return Ok(result);
    }
    
    //добавить актёра - может только Admin
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateActorDto createDto)
    {
        Log.Information("POST /api/actors - создание актёра: {Name} от пользователя {User}", 
            createDto.Name, User.Identity?.Name ?? "unknown");
        
        // Валидация
        if (string.IsNullOrWhiteSpace(createDto.Name) || createDto.Name.Length > 100)
            return BadRequest(new { error = "Name должен быть от 1 до 100 символов" });
        
        if (createDto.BirthYear < 1900 || createDto.BirthYear > 2010)
            return BadRequest(new { error = "BirthYear должен быть от 1900 до 2010" });
        
        var actor = new Actor
        {
            Name = createDto.Name,
            BirthYear = createDto.BirthYear
        };
        
        _context.Actors.Add(actor);
        await _context.SaveChangesAsync();
        
        Log.Information("POST /api/actors - создан актёр {Name} (ID: {ActorId})", createDto.Name, actor.Id);
        
        var result = new ActorDto
        {
            Id = actor.Id,
            Name = actor.Name,
            BirthYear = actor.BirthYear,
            MoviesCount = 0,
            Movies = new List<MovieSimpleDto>()
        };
        
        return CreatedAtAction(nameof(GetById), new { id = actor.Id }, result);
    }
    
    //изменить актёра - может только Admin
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateActorDto updateDto)
    {
        Log.Information("PUT /api/actors/{Id} - обновление актёра от пользователя {User}", 
            id, User.Identity?.Name ?? "unknown");
        
        if (id != updateDto.Id)
            return BadRequest("ID в URL не совпадает с ID в теле запроса");
        
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
            return NotFound();
        
        if (string.IsNullOrWhiteSpace(updateDto.Name) || updateDto.Name.Length > 100)
            return BadRequest(new { error = "Name должен быть от 1 до 100 символов" });
        
        if (updateDto.BirthYear < 1900 || updateDto.BirthYear > 2010)
            return BadRequest(new { error = "BirthYear должен быть от 1900 до 2010" });
        
        string oldName = actor.Name;
        actor.Name = updateDto.Name;
        actor.BirthYear = updateDto.BirthYear;
        
        try
        {
            await _context.SaveChangesAsync();
            Log.Information("PUT /api/actors/{Id} - актёр обновлён: {OldName} -> {NewName}", id, oldName, actor.Name);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Actors.AnyAsync(a => a.Id == id))
                return NotFound();
            throw;
        }
        
        return NoContent();
    }
    
    //удаляем актёра - может только Admin
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        Log.Information("DELETE /api/actors/{Id} - удаление актёра от пользователя {User}", 
            id, User.Identity?.Name ?? "unknown");
        
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
            return NotFound();
        
        _context.Actors.Remove(actor);
        await _context.SaveChangesAsync();
        
        Log.Information("DELETE /api/actors/{Id} - актёр {Name} удалён", id, actor.Name);
        
        return NoContent();
    }
}