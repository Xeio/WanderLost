using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data;

[Owned]
[MessagePack.MessagePackObject]
public record class Item : IComparable<Item>
{
    [MaxLength(40)]
    [MessagePack.Key(0)]
    public string Name { get; init; } = string.Empty;
    [MessagePack.Key(1)]
    public Rarity Rarity { get; init; }

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
