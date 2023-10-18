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
    public MerchantData MerchantData { get; set; } = new();

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

    public void CalculateNextAppearance(string timeZone, DateTimeOffset calculateFrom)
    {
        var serverUtcOffset = TimeZoneInfo.FindSystemTimeZoneById(timeZone).GetUtcOffset(calculateFrom);

        (NextAppearance, AppearanceExpires) = InternalCalculateAppearance(serverUtcOffset, calculateFrom);
        FutureAppearance = InternalCalculateAppearance(serverUtcOffset, AppearanceExpires.AddSeconds(1)).NextAppearance;
    }

    public static readonly TimeSpan MerchantDuration = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30);

    private IEnumerable<TimeSpan> GetSpawnTimes(DateTimeOffset date)
    {
        int dayOfWeek = (int)date.DayOfWeek;
        yield return TimeSpan.FromHours(MerchantData.SpawnTimes[dayOfWeek]);
        yield return TimeSpan.FromHours(MerchantData.SpawnTimes[dayOfWeek] + 12);
    }

    private (DateTimeOffset NextAppearance, DateTimeOffset NextExpires) InternalCalculateAppearance(TimeSpan serverUtcOffset, DateTimeOffset startingTime)
    {
        //Since merchants spawn for 6 hours, the spawn timer may have been in the previous day so check that first
        var dateToCheck = new DateTimeOffset(startingTime.ToOffset(serverUtcOffset).Date.AddDays(-1), serverUtcOffset);
        var nextAppearanceTime = GetSpawnTimes(dateToCheck)
            .Select(apperance => dateToCheck + apperance)
            .Where(time => time >= startingTime - MerchantDuration)
            .FirstOrDefault();

        if (nextAppearanceTime == default)
        {
            dateToCheck = new DateTimeOffset(startingTime.ToOffset(serverUtcOffset).Date, serverUtcOffset);
            //Check current date
            nextAppearanceTime = GetSpawnTimes(dateToCheck)
            .Select(apperance => dateToCheck + apperance)
            .Where(time => time >= startingTime - MerchantDuration)
            .FirstOrDefault();
        }

        if (nextAppearanceTime == default)
        {
            //Next apperance is the following day
            dateToCheck = new DateTimeOffset(startingTime.ToOffset(serverUtcOffset).Date.AddDays(1), serverUtcOffset);
            nextAppearanceTime = GetSpawnTimes(dateToCheck)
                .Select(apperance => new DateTimeOffset(startingTime.ToOffset(serverUtcOffset).Date.AddDays(1), serverUtcOffset) + apperance)
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
