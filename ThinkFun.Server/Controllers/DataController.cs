using Microsoft.AspNetCore.Mvc;
using ThinkFun.Model;

namespace ThinkFun.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DataController
    : ControllerBase
{
    private readonly ILogger<DataController> _logger;

    public DataController(ILogger<DataController> logger)
    {
        _logger = logger;
    }

    [HttpGet("GetDestinations")]
    public IEnumerable<Destination> GetDestinations()
    {
        return DataManager.Instance.Data.Destinations;
    }

    [HttpGet("GetDestinationStaticData/{id}")]
    public IActionResult GetDestinationStaticData(string id)
    {
        if (id == null)
            return BadRequest();

        var data = DataManager.Instance.Data.GetStaticData(id);
        if (data == null)
            return NotFound(id);

        return Ok(data);
    }

    [HttpGet("GetDestinationLiveData/{id}")]
    public IActionResult GetDestinationLiveData(string id)
    {
        if (id == null)
            return BadRequest();

        var data = DataManager.Instance.Data.GetLiveData(id);
        if (data == null)
            return NotFound(id);

        return Ok(data);
    }
}
