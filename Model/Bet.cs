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

        [JsonPropertyName("inzet")]
        public int Inzet { get; set; }

        [JsonPropertyName("user_id")]
        public ulong UserId { get; set; }
    }

    public class BetCheckResult
    {
        public List<BetResult>? Results { get; set; }
    }

    public class BetResult
    {
        public ulong GameId { get; set; }
        public bool Gewonnen { get; set; }
        public bool Gelijkspel { get; set; }
        public string TeamNaam { get; set; } = "";
        public int Inzet { get; set; }
        public int PuntenVeranderd { get; set; }
    }
}