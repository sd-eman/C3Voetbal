using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C3Voetbal.Model
{
    internal class Session
    {
        public static ulong UserId { get; set; }
        public static string UserName { get; set; } = "";
        public static bool IsAdmin { get; set; }
        public static ulong? TeamId { get; set; }
    }
}
