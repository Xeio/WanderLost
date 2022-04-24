﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data
{
    [MessagePack.MessagePackObject]
    public class Vote
    {
        [MessagePack.Key(0)]
        public Guid ActiveMerchantId { get; init; }

        [JsonIgnore]
        [MessagePack.IgnoreMember]
        public ActiveMerchant ActiveMerchant { get; init; } = new();

        [JsonIgnore]
        [MaxLength(60)]
        [MessagePack.IgnoreMember]
        public string ClientId { get; init; } = string.Empty;

        [MessagePack.Key(1)]
        public VoteType VoteType { get; set; }
    }
}
