
using ThinkFun.Model;

namespace ThinkFun;

public class RichEvent
{
    public RichEvent(Event e)
    {
        Event = e;
    }
    
    public readonly Event Event;

    public string Description
    {
        get
        {
            if (Event is StatusChangedEvent sce)
            {
                string attractionName = DataManager.Instance.GetParkElementById(Event.ParkElementId).Name;
                if (attractionName == null)
                    attractionName = Event.UniqueId;

                string statut = sce.NewStatus == Status.OPENED ? "d'ouvrir" : "de fermer";
                return $"{attractionName} vient {statut}.";
            }

            return $"{Event.UniqueId} {Event.GetType()} {Event.ParkId}";
        }
    }

    public string ShortTime
    {
        get
        {
            return Event.Date.ToString("h:mm");
        }
    }

}
