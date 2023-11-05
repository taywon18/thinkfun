using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ThinkFun.Model;
using Wood;

namespace ThinkFun.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController
    : ControllerBase
{

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        if (req == null)
            return BadRequest();

        User user;
        try
        {
            user = await DataStore.Instance.Login(req);
        }
        catch(Exception ex) {
            LogManager.Error($"Got exception while login: {ex}");
            return NotFound();
        }
        if (user == null)
            return NotFound();

        user.PasswordHash = ""; 


        ClaimsIdentity identity = new ClaimsIdentity(new List<Claim>()
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.NameIdentifier, user.Identifier),
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new()
        {
            IsPersistent = true
        });
       
        return Ok(user);
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (req == null)
            return BadRequest();

        User user;
        try
        {
            user = await DataStore.Instance.Register(req);
        }
        catch (Exception ex)
        {
            return NotFound();
        }
        user.PasswordHash = "";


        ClaimsIdentity identity = new ClaimsIdentity(new List<Claim>()
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.NameIdentifier, user.Identifier)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new()
        {
            IsPersistent = true
        });

        SignIn(principal);

        return Ok(user);
    }

    [HttpGet("GetLocalUser")]
    [Authorize]
    public async Task<IActionResult> GetLocalUser()
    {
        User? user = await DataStore.Instance.GetUserFromContext(HttpContext);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("Logout")]
    [Authorize]
    public async Task<IActionResult> GetLogout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }

    [HttpGet("AccessDenied")]
    public async Task<IActionResult> AccessDenied()
    {
        return Unauthorized();
    }
}
