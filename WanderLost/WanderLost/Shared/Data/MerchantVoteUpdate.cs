namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class MerchantVoteUpdate
{
    [MessagePack.Key(0)]
    public Guid Id { get; init; }

    [MessagePack.Key(1)]
    public int Votes { get; init; }
}
