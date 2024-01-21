using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using System.Xml.Linq;
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

    [HttpGet("GetLastEvents/{destinationid}")]
    public async Task<IActionResult> GetLastEvents(string destinationid)
    {
        if (destinationid == null)
            return BadRequest();

        var ret = DataManager.Instance.Data.GetLastEvents(destinationid);

        return Ok(ret);
    }

    [HttpGet("GetParkElementDetail/{destinationid}/{parkid}/{elementid}")]
    public async Task<IActionResult> GetLastEvents(string destinationid, string parkid, string elementid)
    {
        if (destinationid == null)
            return BadRequest();
        if (parkid == null)
            return BadRequest();
        if (elementid == null)
            return BadRequest();

        DateTime now = DateTime.Now;
        
        var lastMesureTsk = DataStore.Instance.GetLastQueue(destinationid, parkid, elementid);

        var todayTsk = DataStore.Instance.GetHistory(
            destinationid,
            parkid,
            elementid,
            (now).Date,
            TimeSpan.FromDays(1),
            TimeSpan.FromHours(1));

        var lastDayTsk = DataStore.Instance.GetHistory(
            destinationid,
            parkid,
            elementid,
            (now - TimeSpan.FromDays(1)).Date,
            TimeSpan.FromDays(1),
            TimeSpan.FromHours(1));

        var sameDayLastWeekTsk = DataStore.Instance.GetHistory(
            destinationid,
            parkid,
            elementid,
            (now - TimeSpan.FromDays(7)).Date,
            TimeSpan.FromDays(1),
            TimeSpan.FromHours(1));

        await Task.WhenAll(todayTsk,lastDayTsk, sameDayLastWeekTsk, lastMesureTsk);

        ParkElementDetail ret = new()
        {
            Today = todayTsk.Result,
            LastDay = lastDayTsk.Result,
            LastWeekSameDay = sameDayLastWeekTsk.Result
        };

        var lastMesure = lastMesureTsk.Result;
        if (lastMesure is not null && lastMesure.LastUpdate.Date == DateTime.Now.Date)
            ret.LastMesure = lastMesure;

        return Ok(ret);
    }
}
