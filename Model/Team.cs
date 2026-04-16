using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C3Voetbal.Model
{
    public class Team
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public int Points { get; set; }
        public ulong UserId { get; set; }
    }
}
