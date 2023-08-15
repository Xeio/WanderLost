using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data;

[Owned]
[MessagePack.MessagePackObject]
public class Item : IEquatable<Item>, IComparable<Item>
{
    [MaxLength(40)]
    [MessagePack.Key(0)]
    public string Name { get; init; } = string.Empty;
    [MessagePack.Key(1)]
    public Rarity Rarity { get; init; }

    public bool Equals(Item? other)
    {
        return other is not null &&
            Name == other.Name &&
            Rarity == other.Rarity;
    }

    public int CompareTo(Item? other)
    {
        int cmp = Rarity.CompareTo(other?.Rarity);
        if(cmp != 0)
        {
            return cmp;
        }
        return string.Compare(Name, other?.Name);
    }
}
