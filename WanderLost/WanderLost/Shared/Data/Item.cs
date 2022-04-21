using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data
{
    [Owned]
    public class Item : IEquatable<Item>
    {
        [MaxLength(40)]
        public string Name { get; init; } = string.Empty;
        public Rarity Rarity { get; init; }

        public bool Equals(Item? other)
        {
            return other is not null &&
                Name == other.Name && 
                Rarity == other.Rarity;
        }
    }
}
