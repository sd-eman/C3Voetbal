using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;         // NIEUW
using System.Net.Http.Json;    // NIEUW
using System.Runtime.InteropServices.WindowsRuntime;
using C3Voetbal.Data;
using C3Voetbal.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;

namespace C3Voetbal
{
    public class GameViewModel
    {
        public ulong Id { get; set; }
        public string DisplayName { get; set; }
        public string Time { get; set; }
        public ulong Team1Id { get; set; }
        public ulong Team2Id { get; set; }
    }
    public sealed partial class MainWindow : Window
    {
        private GameViewModel _selectedGame;

        private static readonly HttpClient _client = new HttpClient  // NIEUW
        {
            BaseAddress = new Uri("http://localhost:5000/api/")
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadGames();
        }
        private void LoadGames()
        {
            using var db = new C3VoetbalDbContext();
            var teams = db.Teams.ToList();
            var upcoming = db.Games
                .Where(g => g.Team1Score == null && g.Team2Score == null)
                .ToList()
                .Select(g => new GameViewModel
                {
                    Id = g.Id,
                    DisplayName = $"{teams.FirstOrDefault(t => t.Id == g.Team1Id)?.Name ?? "Team 1"} vs {teams.FirstOrDefault(t => t.Id == g.Team2Id)?.Name ?? "Team 2"}",
                    Time = g.Time,
                    Team1Id = g.Team1Id,
                    Team2Id = g.Team2Id
                }).ToList();
            GamesListView.ItemsSource = upcoming;
        }
        private void GamesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedGame = GamesListView.SelectedItem as GameViewModel;
            if (_selectedGame == null) return;
            using var db = new C3VoetbalDbContext();
            var t1 = db.Teams.FirstOrDefault(t => t.Id == _selectedGame.Team1Id)?.Name ?? "Team 1";
            var t2 = db.Teams.FirstOrDefault(t => t.Id == _selectedGame.Team2Id)?.Name ?? "Team 2";
            SelectedGameText.Text = _selectedGame.DisplayName;
            RadioTeam1.Content = $"{t1} wint";
            RadioTeam2.Content = $"{t2} wint";
            RadioTeam1.IsEnabled = true;
            RadioDraw.IsEnabled = true;
            RadioTeam2.IsEnabled = true;
            PlaceBetButton.IsEnabled = true;
            BetFeedbackText.Text = "";
        }
        private async void PlaceBetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGame == null) return;
            BetOutcome? outcome = null;
            if (RadioTeam1.IsChecked == true) outcome = BetOutcome.Team1Wins;
            else if (RadioDraw.IsChecked == true) outcome = BetOutcome.Draw;
            else if (RadioTeam2.IsChecked == true) outcome = BetOutcome.Team2Wins;
            if (outcome == null)
            {
                BetFeedbackText.Text = "Kies eerst een uitkomst.";
                return;
            }
            var response = await _client.PostAsJsonAsync("bets", new
            {
                game_id = _selectedGame.Id,
                predicted_outcome = outcome.Value
            });
            if (response.IsSuccessStatusCode)
                BetFeedbackText.Text = "✓ Gok geplaatst!";
            PlaceBetButton.IsEnabled = false;
            RadioTeam1.IsEnabled = false;
            RadioDraw.IsEnabled = false;
            RadioTeam2.IsEnabled = false;
        }
    }
}
