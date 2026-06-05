namespace BGPLite.Protocol;

public sealed class BgpNotificationMessage : BgpMessage
{
    public override BgpMessageType Type => BgpMessageType.Notification;
    public byte ErrorCode { get; init; }
    public byte SubErrorCode { get; init; }
    public byte[]? Data { get; init; }
}
