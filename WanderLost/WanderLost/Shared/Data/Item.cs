using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data
{
    [Owned]
    public class Item
    {
        [MaxLength(40)]
        public string Name { get; init; } = string.Empty;
        public Rarity Rarity { get; init; }
    }
}
