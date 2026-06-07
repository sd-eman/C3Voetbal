using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C3Voetbal.Model
{
    public class BetHistoryItem
    {
        public string WedstrijdNaam { get; set; }
        public string Voorspelling { get; set; }
        public string Inzet { get; set; }
        public string ResultaatText { get; set; }
        public SolidColorBrush ResultaatKleur { get; set; }
    }
}
