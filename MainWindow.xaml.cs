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
using System.Net.Http;         // NIEUW
using System.Net.Http.Json;    // NIEUW
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;
using Windows.System;

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
        private UserDto _loggedInUser;      
        private int _userPoints = 20;       
        private int _inzet = 1;             

        private static readonly HttpClient _client = new HttpClient  // NIEUW
        {
            BaseAddress = new Uri("http://localhost:5000/api/")
        };

        public MainWindow(UserDto user)
        {
            InitializeComponent();
            _loggedInUser = user;
            System.Diagnostics.Debug.WriteLine($"Ingelogde user id: {_loggedInUser?.Id}");
            LoadGames();
            LoadUserPoints();
            _ = CheckBetResultsAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    System.Diagnostics.Debug.WriteLine($"CheckBets fout: {t.Exception?.Message}");
            });

        }
        private async void LoadGames()
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var games = await _client.GetFromJsonAsync<List<GameViewModel>>("games", options);
            GamesListView.ItemsSource = games;
        }

        private void LoadUserPoints()
        {
            using var db = new C3VoetbalDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == _loggedInUser.Id);
            if (user != null)
            {
                _userPoints = user.Points;
                PuntenText.Text = $"Punten: {_userPoints}";
                if (InzetNumberBox != null)
                    InzetNumberBox.Maximum = _userPoints;
            }
        }

        private async Task CheckBetResultsAsync()
        {
            var response = await _client.GetAsync($"bets/check?user_id={_loggedInUser.Id}");
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<BetCheckResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data?.Results == null || data.Results.Count == 0) return;

            string melding = "";
            foreach (var result in data.Results)
            {
                if (result.Gewonnen)
                    melding += $"🏆 Gefeliciteerd! {result.TeamNaam} heeft gewonnen! +{result.PuntenVeranderd} punten\n";
                else if (result.Gelijkspel)
                    melding += $"🤝 Gelijkspel bij {result.TeamNaam}! Geen punten verloren.\n";
                else
                    melding += $"❌ Helaas! {result.TeamNaam} heeft verloren. {result.PuntenVeranderd} punten\n";
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                LoadUserPoints();
                BetFeedbackText.Text = melding.Trim();
            });
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
            // Laat inzet panel zien
            InzetPanel.Visibility = Visibility.Visible;
            BetFeedbackText.Text = "";
        }

        private void InzetNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (ErrorText == null || BevestigButton == null) return;

            if (double.IsNaN(sender.Value))
            {
                sender.Value = 1;
                return;
            }

            _inzet = (int)sender.Value;

            // Maximum is jouw huidige punten
            sender.Maximum = _userPoints;

            if (_inzet > _userPoints)
            {
                ErrorText.Text = $"Je hebt maar {_userPoints} punten!";
                ErrorText.Visibility = Visibility.Visible;
                BevestigButton.IsEnabled = false;
            }
            else if (_inzet < 1)
            {
                ErrorText.Text = "Minimaal 1 punt inzetten!";
                ErrorText.Visibility = Visibility.Visible;
                BevestigButton.IsEnabled = false;
            }
            else
            {
                ErrorText.Visibility = Visibility.Collapsed;
                BevestigButton.IsEnabled = true;
            }
        }

        private async void BevestigButton_Click(object sender, RoutedEventArgs e)
        {
            BetOutcome outcome = BetOutcome.Draw;
            if (RadioTeam1.IsChecked == true) outcome = BetOutcome.Team1Wins;
            else if (RadioTeam2.IsChecked == true) outcome = BetOutcome.Team2Wins;
            System.Diagnostics.Debug.WriteLine($"Bet versturen: game_id={_selectedGame.Id}, user_id={_loggedInUser.Id}, inzet={_inzet}");

            var response = await _client.PostAsJsonAsync("bets", new
            {
                user_id = Session.UserId,
                game_id = _selectedGame.Id,
                user_id = _loggedInUser.Id,
                predicted_outcome = outcome,
                inzet = _inzet
            });

            var responseBody = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Bet response: {response.StatusCode} - {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                BetFeedbackText.Text = "✓ Gok geplaatst! Punten worden verwerkt als de wedstrijd gespeeld is.";
                InzetPanel.Visibility = Visibility.Collapsed;
                PlaceBetButton.IsEnabled = false;
                RadioTeam1.IsEnabled = false;
                RadioDraw.IsEnabled = false;
                RadioTeam2.IsEnabled = false;
            }
            else
            {
                BetFeedbackText.Text = "❌ Er ging iets mis, probeer opnieuw.";
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DashboardPage(_loggedInUser);
            dashboard.Activate();
        }
    }
}

