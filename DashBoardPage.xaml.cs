using C3Voetbal.Data;
using C3Voetbal.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace C3Voetbal
{
    public sealed partial class DashboardPage : Window
    {
        private UserDto _loggedInUser;

        public DashboardPage(UserDto user)
        {
            InitializeComponent();
            _loggedInUser = user;
            LoadBetHistory();
        }

        private void LoadBetHistory()
        {
            using var appDb = new AppDbContext();
            using var mainDb = new C3VoetbalDbContext();

            var bets = appDb.Bets
                .Where(b => b.UserId == (ulong)_loggedInUser.Id)
                .ToList();

            var teams = mainDb.Teams.ToList();
            var games = mainDb.Games.ToList();

            var items = bets.Select(bet =>
            {
                var game = games.FirstOrDefault(g => g.Id == bet.GameId);
                var team1 = teams.FirstOrDefault(t => t.Id == game?.Team1Id)?.Name ?? "Team 1";
                var team2 = teams.FirstOrDefault(t => t.Id == game?.Team2Id)?.Name ?? "Team 2";

                string voorspelling = bet.PredictedOutcome switch
                {
                    BetOutcome.Team1Wins => $"{team1} wint",
                    BetOutcome.Draw => "Gelijkspel",
                    BetOutcome.Team2Wins => $"{team2} wint",
                    _ => "?"
                };

                string resultaatText;
                SolidColorBrush kleur;

                if (bet.Won == null)
                {
                    resultaatText = "⏳ Nog bezig";
                    kleur = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 204, 221, 170));
                }
                else if (bet.Won == true)
                {
                    resultaatText = $"+{bet.Inzet * 2} punten";
                    kleur = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 220, 100));
                }
                else
                {
                    resultaatText = $"-{bet.Inzet} punten";
                    kleur = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 80, 80));
                }

                return new BetHistoryItem
                {
                    WedstrijdNaam = game != null ? $"{team1} vs {team2}" : "Onbekend",
                    Voorspelling = voorspelling,
                    Inzet = $"{bet.Inzet} punt",
                    ResultaatText = resultaatText,
                    ResultaatKleur = kleur
                };
            }).ToList();

            BetHistoryList.ItemsSource = items;
        }

        private void TerugButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
