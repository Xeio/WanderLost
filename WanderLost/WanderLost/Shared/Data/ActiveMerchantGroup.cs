using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class ActiveMerchantGroup
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [MessagePack.Key(0)]
    public int Id { get; set; }

    [MaxLength(20)]
    [MessagePack.Key(1)]
    public string Server { get; set; } = string.Empty;

    [NotMapped]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public MerchantData MerchantData { get; init; } = new();

    private string _merchantName = string.Empty;
    [MaxLength(20)]
    [MessagePack.Key(2)]
    public string MerchantName
    {
        get
        {
            //If merchant group came across network, it won't have data but the name will be serialized as its own property
            if (!string.IsNullOrWhiteSpace(MerchantData.Name))
            {
                return MerchantData.Name;
            }
            else
            {
                return _merchantName;
            }
        }
        set
        {
            _merchantName = value;
        }
    }

    [MessagePack.Key(3)]
    public List<ActiveMerchant> ActiveMerchants { get; init; } = new List<ActiveMerchant>();

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public DateTimeOffset NextAppearance { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public DateTimeOffset AppearanceExpires { get; set; }

    [NotMapped]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public DateTimeOffset FutureAppearance { get; set; }

    [NotMapped]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool IsActive => DateTimeOffset.UtcNow >= NextAppearance && DateTimeOffset.UtcNow < AppearanceExpires;

    public void CalculateNextAppearance(string timeZone)
    {
        var serverUtcOffset = TimeZoneInfo.FindSystemTimeZoneById(timeZone).GetUtcOffset(DateTime.UtcNow);

        (NextAppearance, AppearanceExpires) = InternalCalculateAppearance(serverUtcOffset);
        FutureAppearance = InternalCalculateAppearance(serverUtcOffset, AppearanceExpires.AddSeconds(1)).NextAppearance;
    }

    public static readonly TimeSpan MerchantDuration = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30);

    private (DateTimeOffset NextAppearance, DateTimeOffset NextExpires) InternalCalculateAppearance(TimeSpan serverUtcOffset, DateTimeOffset? startingTime = null)
    {
        if (startingTime is null)
        {
            startingTime = DateTimeOffset.Now;
        }

        var nextAppearanceTime = MerchantData.AppearanceTimes
            .Select(apperance => new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(serverUtcOffset).Date, serverUtcOffset) + apperance)
            .Where(time => time >= startingTime - MerchantDuration)
            .FirstOrDefault();

        if (nextAppearanceTime == default)
        {
            //Next apperance is the following day
            nextAppearanceTime = MerchantData.AppearanceTimes
                .Select(apperance => new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(serverUtcOffset).Date.AddDays(1), serverUtcOffset) + apperance)
                .Where(time => time >= startingTime - MerchantDuration)
                .FirstOrDefault();
        }

        return (nextAppearanceTime, nextAppearanceTime + MerchantDuration);
    }

    public void ClearInstances()
    {
        ActiveMerchants.Clear();
    }
}
