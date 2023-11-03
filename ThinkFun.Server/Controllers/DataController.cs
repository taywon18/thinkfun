using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using ThinkFun.Model;

namespace ThinkFun.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DataController
    : ControllerBase
{
    public DataController()
    {
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

    [HttpGet("GetHistory/{id}/{from}/{to}")]
    public async Task<IActionResult> GetDestinationLiveData(string id, string from, string to)
    {
        if (id == null)
            return BadRequest();

        if (from == null)
            return BadRequest();
        from = Uri.UnescapeDataString(from);
        DateTime fromDate = DateTime.Parse(from);

        DateTime toDate;

        if (to == null)
            return BadRequest();
        to = Uri.UnescapeDataString(to);

        if (to == "now")
            toDate = DateTime.Now;
        else
            toDate = DateTime.Parse(to);

        List<Queue> data = new();
        await foreach (var i in DataStore.Instance.Get(id, fromDate, toDate))
            if(i is Queue)
                data.Add((Queue)i);

        return Ok(data);
    }

    [HttpGet("GetLastEvents/{destinationid}")]
    public async Task<IActionResult> GetLastEvents(string destinationid)
    {
        if (destinationid == null)
            return BadRequest();

        var ret = DataManager.Instance.Data.GetLastEvents(destinationid);

        return Ok(ret);
    }
}
