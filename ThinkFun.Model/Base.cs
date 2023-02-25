namespace ThinkFun.Model;

public abstract class Base
{
    public string UniqueIdentifier { get; set;  } = "";
    public Dictionary<string, string> ExternalIds { get; set; } = new Dictionary<string, string>();
}
