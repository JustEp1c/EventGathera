using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EventGathera.Presentation.Controllers;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService 
            ?? throw new ArgumentNullException(nameof(userService));
    }

    [HttpPost("auth/register")]
    public async Task<IActionResult> Register(string login, string password, Roles role = Roles.User)
    {
        await _userService.Register(login, password, role);

        return NoContent();
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(string login, string password)
    {
        var token = await _userService.Login(login, password);

        return Ok(token);
    }
}
