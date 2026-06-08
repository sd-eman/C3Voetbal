using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace C3Voetbal.Model
{
    public enum BetOutcome
    {
        Team1Wins = 0,
        Draw = 1,
        Team2Wins = 2
    }

    public class Bet
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("game_id")]
        public ulong GameId { get; set; }

        [JsonPropertyName("predicted_outcome")]
        public BetOutcome PredictedOutcome { get; set; }

        [JsonPropertyName("won")]
        public bool? Won { get; set; }

        [JsonPropertyName("user_id")]
        public ulong UserId { get; set; }
    }
}