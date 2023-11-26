using Microsoft.AspNetCore.Mvc;
using ThinkFun.Model;

namespace ThinkFun.Server.Controllers;


[ApiController]
[Route("[controller]")]
public class HistoryController
       : ControllerBase
{
    [HttpGet("Raw/{destination}/{park}/{element}/{from}/{to}")]
    public async Task<IActionResult> GetDestinationLiveData(string destination, string park, string element, string from, string to)
    {
        if (destination == null || park == null || element == null)
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
        await foreach (var i in DataStore.Instance.Get(destination, park, element, fromDate, toDate))
            if (i is Queue)
                data.Add((Queue)i);

        return Ok(data);
    }

    [HttpGet("Averages/{destination}/{park}/{element}/{when}")]
    public async Task<IActionResult> GetDestinationLiveData(string destination, string park, string element, string when)
    {
        if (destination == null || park == null || element == null)
            return BadRequest();

        if (when == null)
            return BadRequest();
        when = Uri.UnescapeDataString(when);
        var whenDate = DateTime.Parse(when);
        whenDate = whenDate.Ceil(TimeSpan.FromDays(1));
        var endDate = whenDate.AddDays(1) - TimeSpan.FromTicks(1);

        var history = await DataStore.Instance.GetHistory(destination, park, element, whenDate, endDate, TimeSpan.FromHours(1));
        return Ok(history);
    }

}
