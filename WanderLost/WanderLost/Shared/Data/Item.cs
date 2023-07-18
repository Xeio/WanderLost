﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data;

[Owned]
[MessagePack.MessagePackObject]
public class Item : IEquatable<Item>
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
}
