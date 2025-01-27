namespace RobloxLagswitch;

public class InputTrigger
{
    public enum TriggerType
    {
        Key,
        MouseButton
    }

    public TriggerType Type { get; set; }
    public int Code { get; set; }
}
