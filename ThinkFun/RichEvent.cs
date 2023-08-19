
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

    public string Icon
    {
        get
        {
            if (Event is StatusChangedEvent sce)
            {
                if (sce.NewStatus == Status.OPENED)
                    return "play.png";

                if (sce.NewStatus == Status.CLOSED)
                    return "close.png";

                if (sce.NewStatus == Status.DOWN)
                    return "broken.png";
            }

            return "error.png";
        }
    }

    public Color Color
    {
        get
        {
            if (Event is StatusChangedEvent sce)
            {
                if (sce.NewStatus == Status.OPENED)
                    return Color.FromRgb(0,255,0);

                if (sce.NewStatus == Status.CLOSED)
                    return Color.FromRgb(255, 0, 0);

                if (sce.NewStatus == Status.DOWN)
                    return Color.FromRgb(0, 255, 0);
            }

            return Color.FromRgb(100, 100, 100); ;
        }
    }

}
