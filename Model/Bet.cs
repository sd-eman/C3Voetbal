using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public ulong Id { get; set; }
        public ulong GameId { get; set; }
        public BetOutcome PredictedOutcome { get; set; }
        public bool? Won { get; set; }
    }
}