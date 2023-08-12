using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ThinkFun.Server.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class TripController
    : ControllerBase
{
    [HttpPost(nameof(Create))]
    [Authorize]
    public async Task<IActionResult> Create()
    {
        throw new NotImplementedException();
    }

    [HttpGet(nameof(GetMine))]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        throw new NotImplementedException();
    }

    [HttpGet(nameof(Get))]
    [Authorize]
    public async Task<IActionResult> Get(string tripid)
    {
        throw new NotImplementedException();
    }

    [HttpDelete(nameof(Delete))]
    [Authorize]
    public async Task<IActionResult> Delete(string tripid)
    {
        throw new NotImplementedException();
    }

    [HttpGet(nameof(Invite))]
    [Authorize]
    public async Task<IActionResult> Invite(string tripid, string userid)
    {
        throw new NotImplementedException();
    }


    [HttpGet(nameof(Exclude))]
    [Authorize]
    public async Task<IActionResult> Exclude(string tripid, string userid)
    {
        throw new NotImplementedException();
    }
}
