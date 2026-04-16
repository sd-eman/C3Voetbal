using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C3Voetbal.Model
{
    public class Game
    {
        public ulong Id { get; set; }
        public ulong Team1Id { get; set; }
        public ulong Team2Id { get; set; }
        public int? Team1Score { get; set; }
        public int? Team2Score { get; set; }
        public string Field { get; set; }
        public ulong RefereeId { get; set; }
        public string Time { get; set; }
    }
}
