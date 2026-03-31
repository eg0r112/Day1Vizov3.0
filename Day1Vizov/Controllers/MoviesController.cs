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
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public MoviesController(AppDbContext context)
    {
        _context = context;
    }
    
    //получить список фильмов - доступно всем кто авторизован!
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        Log.Information("GET /api/movies - запрос всех фильмов от пользователя {User}", 
            User.Identity?.Name ?? "unknown");
        
        var movies = await _context.Movies
            .Include(m => m.MovieActors)
            .ThenInclude(ma => ma.Actor)
            .ToListAsync();
        
        var result = movies.Select(m => new MovieDto
        {
            Id = m.Id,
            Title = m.Title,
            Year = m.Year,
            Genre = m.Genre,
            Description = m.Description,
            Actors = m.MovieActors.Select(ma => new ActorInMovieResponseDto
            {
                Id = ma.Actor.Id,
                Name = ma.Actor.Name,
                Role = ma.Role
            }).ToList()
        });
        
        return Ok(result);
    }
    
    //получить инфу о конкретном фильме - доступно всем кто авторизован!
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Log.Information("GET /api/movies/{Id} - запрос фильма от пользователя {User}", 
            id, User.Identity?.Name ?? "unknown");
        
        var movie = await _context.Movies
            .Include(m => m.MovieActors)
            .ThenInclude(ma => ma.Actor)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (movie == null)
            return NotFound();
        
        var result = new MovieDto
        {
            Id = movie.Id,
            Title = movie.Title,
            Year = movie.Year,
            Genre = movie.Genre,
            Description = movie.Description,
            Actors = movie.MovieActors.Select(ma => new ActorInMovieResponseDto
            {
                Id = ma.Actor.Id,
                Name = ma.Actor.Name,
                Role = ma.Role
            }).ToList()
        };
        
        return Ok(result);
    }
    
    //добавить фильм - может только Admin
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMovieDto createDto)
    {
        Log.Information("POST /api/movies - создание фильма: {Title} от пользователя {User}", 
            createDto.Title, User.Identity?.Name ?? "unknown");
        
        if (string.IsNullOrWhiteSpace(createDto.Title) || createDto.Title.Length > 100)
            return BadRequest(new { error = "Title должен быть от 1 до 100 символов" });
        
        if (createDto.Year < 1900 || createDto.Year > 2030)
            return BadRequest(new { error = "Year должен быть от 1900 до 2030" });
        
        var actorIds = createDto.Actors.Select(a => a.ActorId).ToList();
        var existingActors = await _context.Actors
            .Where(a => actorIds.Contains(a.Id))
            .ToListAsync();
        
        if (existingActors.Count != actorIds.Count)
        {
            var missingIds = actorIds.Except(existingActors.Select(a => a.Id));
            return BadRequest(new { error = $"Актёры с ID {string.Join(", ", missingIds)} не найдены" });
        }
        
        var movie = new Movie
        {
            Title = createDto.Title,
            Year = createDto.Year,
            Genre = createDto.Genre,
            Description = createDto.Description
        };
        
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        
        foreach (var actorDto in createDto.Actors)
        {
            var movieActor = new MovieActor
            {
                MovieId = movie.Id,
                ActorId = actorDto.ActorId,
                Role = actorDto.Role
            };
            _context.MovieActors.Add(movieActor);
        }
        
        await _context.SaveChangesAsync();
        
        var result = await _context.Movies
            .Include(m => m.MovieActors)
            .ThenInclude(ma => ma.Actor)
            .FirstOrDefaultAsync(m => m.Id == movie.Id);
        
        var response = new MovieDto
        {
            Id = result.Id,
            Title = result.Title,
            Year = result.Year,
            Genre = result.Genre,
            Description = result.Description,
            Actors = result.MovieActors.Select(ma => new ActorInMovieResponseDto
            {
                Id = ma.Actor.Id,
                Name = ma.Actor.Name,
                Role = ma.Role
            }).ToList()
        };
        
        Log.Information("POST /api/movies - фильм {Title} создан (ID: {MovieId})", createDto.Title, movie.Id);
        
        return CreatedAtAction(nameof(GetById), new { id = movie.Id }, response);
    }
    
    //изменяем инфу о фильме - может только Admin
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMovieDto updateDto)
    {
        Log.Information("PUT /api/movies/{Id} - обновление фильма от пользователя {User}", 
            id, User.Identity?.Name ?? "unknown");
        
        if (id != updateDto.Id)
            return BadRequest("ID в URL не совпадает с ID в теле запроса");
        
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
            return NotFound();
        
        if (string.IsNullOrWhiteSpace(updateDto.Title) || updateDto.Title.Length > 100)
            return BadRequest(new { error = "Title должен быть от 1 до 100 символов" });
        
        if (updateDto.Year < 1900 || updateDto.Year > 2030)
            return BadRequest(new { error = "Year должен быть от 1900 до 2030" });
        
        movie.Title = updateDto.Title;
        movie.Year = updateDto.Year;
        movie.Genre = updateDto.Genre;
        movie.Description = updateDto.Description;
        
        try
        {
            await _context.SaveChangesAsync();
            Log.Information("PUT /api/movies/{Id} - фильм обновлён", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Movies.AnyAsync(m => m.Id == id))
                return NotFound();
            throw;
        }
        
        return NoContent();
    }
    
    //удаляем фильм - может только Admin
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        Log.Information("DELETE /api/movies/{Id} - удаление фильма от пользователя {User}", 
            id, User.Identity?.Name ?? "unknown");
        
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
            return NotFound();
        
        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();
        
        Log.Information("DELETE /api/movies/{Id} - фильм {Title} удалён", id, movie.Title);
        
        return NoContent();
    }
}