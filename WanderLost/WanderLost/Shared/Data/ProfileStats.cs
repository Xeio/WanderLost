
namespace WanderLost.Shared.Data
{
    [MessagePack.MessagePackObject]
    public class ProfileStats
    {
        [MessagePack.Key(0)]
        public string PrimaryServer { get; set; } = string.Empty;
        [MessagePack.Key(1)]
        public int TotalUpvotes { get; set; }
        [MessagePack.Key(2)]
        public int UpvotedMerchats { get; set; }
        //Message pack doesn't yet support DateOnly, should be in next stable release
        //Could alternatively change these to DateTimes...
        //[MessagePack.Key(3)]
        //public DateOnly? OldestSubmission { get; set; }
        //[MessagePack.Key(4)]
        //public DateOnly? NewestSubmission { get; set; }
    }
}
