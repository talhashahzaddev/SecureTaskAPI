using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecureTaskManagement.Data;
using SecureTaskManagement.DTOs;
using SecureTaskManagement.Hubs;
using SecureTaskManagement.Models;
using System.Data;
using System.Security.Claims;

namespace SecureTaskManagement.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize] // all endpoints require a valid JWT
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<TaskHub> _hubContext;

    public TasksController(AppDbContext db, IHubContext<TaskHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    // Helper: get the logged-in user's ID from the JWT
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetUserRole() =>
        User.FindFirstValue(ClaimTypes.Role)!;

    // GET /api/tasks
    // Admin sees all tasks, User sees only their own
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var tasks = role == "Admin"
            ? await _db.Tasks.Include(t => t.CreatedBy).ToListAsync()
            : await _db.Tasks.Where(t => t.CreatedByUserId == userId).ToListAsync();

        return Ok(tasks);
    }

    // POST /api/tasks
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            CreatedByUserId = GetUserId()
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        // Broadcast to ALL connected clients in real-time
        await _hubContext.Clients.All.SendAsync("TaskCreated", new
        {
            task.Id,
            task.Title,
            task.Description,
            task.CreatedByUserId,
            CreatedBy = User.FindFirstValue(ClaimTypes.Name)
        });

        return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
    }

    // DELETE /api/tasks/{id} — Admin only
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task == null)
            return NotFound(new { message = "Task not found." });

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Task deleted successfully." });
    }
}