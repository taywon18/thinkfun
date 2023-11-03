namespace ThinkFun.Server;

//Src: grenade from https://stackoverflow.com/questions/1393696/rounding-datetime-objects
public static class DateExtension
{
    public static DateTime Round(this DateTime date, TimeSpan span)
    {
        long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
        return new DateTime(ticks * span.Ticks);
    }
    public static DateTime Floor(this DateTime date, TimeSpan span)
    {
        long ticks = (date.Ticks / span.Ticks);
        return new DateTime(ticks * span.Ticks);
    }
    public static DateTime Ceil(this DateTime date, TimeSpan span)
    {
        long ticks = (date.Ticks + span.Ticks - 1) / span.Ticks;
        return new DateTime(ticks * span.Ticks);
    }
}
