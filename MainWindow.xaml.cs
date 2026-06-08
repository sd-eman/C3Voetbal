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
    public class TeamDto
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class GameViewModel
    {
        public ulong Id { get; set; }
        public string Time { get; set; } = "";
        public string Field { get; set; } = "";
        public TeamDto? Team1 { get; set; }
        public TeamDto? Team2 { get; set; }

        public string DisplayName => $"{Team1?.Name ?? "?"} vs {Team2?.Name ?? "?"}";
        public ulong Team1Id => Team1?.Id ?? 0;
        public ulong Team2Id => Team2?.Id ?? 0;
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
        private async void LoadGames()
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var games = await _client.GetFromJsonAsync<List<GameViewModel>>("games", options);
            GamesListView.ItemsSource = games;
        }
        private void GamesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedGame = GamesListView.SelectedItem as GameViewModel;
            if (_selectedGame == null) return;

            SelectedGameText.Text = _selectedGame.DisplayName;
            RadioTeam1.Content = $"{_selectedGame.Team1?.Name ?? "Team 1"} wint";
            RadioTeam2.Content = $"{_selectedGame.Team2?.Name ?? "Team 2"} wint";
            RadioTeam1.IsEnabled = true;
            RadioDraw.IsEnabled = true;
            RadioTeam2.IsEnabled = true;
            PlaceBetButton.IsEnabled = true;
            BetFeedbackText.Text = "";
        }
        private async void PlaceBetButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Session.UserId = {Session.UserId}");
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
                user_id = Session.UserId,
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
