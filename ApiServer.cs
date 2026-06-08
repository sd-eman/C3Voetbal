using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using C3Voetbal.Data;
using C3Voetbal.Model;

namespace C3Voetbal
{
    public class ApiServer
    {
        private readonly HttpListener _listener = new HttpListener();

        public ApiServer()
        {
            _listener.Prefixes.Add("http://localhost:5000/api/");
        }

        public async Task StartAsync()
        {
            _listener.Start();
            while (true)
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.ContentType = "application/json";
            response.Headers.Add("Access-Control-Allow-Origin", "*");

            string responseJson = "{}";

            try
            {
                // POST /api/login
                if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/api/login")
                {
                    using var reader = new System.IO.StreamReader(request.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<LoginRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    using var db = new C3VoetbalDbContext();
                    var user = db.Users.FirstOrDefault(u => u.Email == data!.Email);

                    if (user == null)
                    {
                        response.StatusCode = 401;
                        responseJson = JsonSerializer.Serialize(new { message = "Gebruiker niet gevonden" });
                    }
                    else if (!BCrypt.Net.BCrypt.Verify(data!.Password, user.Password))
                    {
                        response.StatusCode = 401;
                        responseJson = JsonSerializer.Serialize(new { message = "Wachtwoord onjuist" });
                    }
                    else
                    {
                        response.StatusCode = 200;
                        responseJson = JsonSerializer.Serialize(new
                        {
                            user = new
                            {
                                user.Id,
                                user.Name,
                                user.Email,
                                user.IsAdmin,
                                user.TeamId
                            }
                        });
                    }
                }

                // GET /api/games
                else if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/api/games")
                {
                    using var db = new C3VoetbalDbContext();
                    var teams = db.Teams.ToList();
                    var games = db.Games
                        .Where(g => g.Team1Score == null && g.Team2Score == null)
                        .ToList()
                        .Select(g => new
                        {
                            g.Id,
                            g.Time,
                            g.Field,
                            Team1 = teams.FirstOrDefault(t => t.Id == g.Team1Id),
                            Team2 = teams.FirstOrDefault(t => t.Id == g.Team2Id),
                        });

                    responseJson = JsonSerializer.Serialize(games);
                }

                // GET /api/bets/check
                else if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/api/bets/check")
                {
                    var query = request.Url.Query;
                    var userId = ulong.Parse(System.Web.HttpUtility.ParseQueryString(query)["user_id"]!);

                    using var appDb = new AppDbContext();
                    using var mainDb = new C3VoetbalDbContext();

                    var bets = appDb.Bets
                        .Where(b => b.UserId == userId && b.Won == null)
                        .ToList();

                    var results = new List<object>();

                    foreach (var bet in bets)
                    {
                        var game = mainDb.Games.FirstOrDefault(g => g.Id == bet.GameId);

                        // Sla over als wedstrijd niet bestaat, nog niet gespeeld of geen score
                        if (game == null) continue;
                        if (game.Date == null || game.Date > DateTime.Now) continue;
                        if (game.Team1Score == null || game.Team2Score == null) continue;

                        // Bepaal uitslag
                        BetOutcome uitslag;
                        bool gelijkspel = false;

                        if (game.Team1Score > game.Team2Score) uitslag = BetOutcome.Team1Wins;
                        else if (game.Team1Score < game.Team2Score) uitslag = BetOutcome.Team2Wins;
                        else
                        {
                            uitslag = BetOutcome.Draw;
                            gelijkspel = true;
                        }

                        bool gewonnen = bet.PredictedOutcome == uitslag;
                        bet.Won = gewonnen;

                        // Teamnaam voor melding
                        var teams = mainDb.Teams.ToList();
                        string teamNaam = "";
                        if (bet.PredictedOutcome == BetOutcome.Team1Wins)
                            teamNaam = teams.FirstOrDefault(t => t.Id == game.Team1Id)?.Name ?? "Team 1";
                        else if (bet.PredictedOutcome == BetOutcome.Team2Wins)
                            teamNaam = teams.FirstOrDefault(t => t.Id == game.Team2Id)?.Name ?? "Team 2";
                        else
                            teamNaam = "Gelijkspel";

                        // Punten berekenen
                        var user = mainDb.Users.FirstOrDefault(u => u.Id == userId);
                        int puntenVeranderd = 0;

                        if (gelijkspel)
                        {
                            puntenVeranderd = 0; // Gelijkspel → geen punten verlies
                        }
                        else if (gewonnen)
                        {
                            puntenVeranderd = bet.Inzet * 2;
                            if (user != null) user.Points += puntenVeranderd;
                        }
                        else
                        {
                            puntenVeranderd = -bet.Inzet;
                            if (user != null) user.Points += puntenVeranderd;
                        }

                        results.Add(new
                        {
                            GameId = bet.GameId,
                            Gewonnen = gewonnen,
                            Gelijkspel = gelijkspel,
                            TeamNaam = teamNaam,
                            Inzet = bet.Inzet,
                            PuntenVeranderd = puntenVeranderd
                        });
                    }

                    appDb.SaveChanges();
                    mainDb.SaveChanges();

                    responseJson = JsonSerializer.Serialize(new { results });
                }

                // POST /api/bets
                else if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/api/bets")
                {
                    using var reader = new System.IO.StreamReader(request.InputStream);
                    var body = await reader.ReadToEndAsync();

                    var betRequest = JsonSerializer.Deserialize<BetRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var bet = new Bet
                    {
                        GameId = Convert.ToUInt64(betRequest!.GameId),
                        UserId = Convert.ToUInt64(betRequest.UserId),
                        PredictedOutcome = (BetOutcome)betRequest.PredictedOutcome,
                        Inzet = betRequest.Inzet,
                        Won = null
                    };

                    using var db = new AppDbContext();
                    db.Bets.Add(bet);
                    db.SaveChanges();

                    response.StatusCode = 201;
                    responseJson = JsonSerializer.Serialize(new { message = "Gok geplaatst" });
                }

                else
                {
                    response.StatusCode = 404;
                    responseJson = JsonSerializer.Serialize(new { message = "Niet gevonden" });
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                responseJson = JsonSerializer.Serialize(new { message = ex.Message });
            }

            var buffer = Encoding.UTF8.GetBytes(responseJson);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();
        }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class BetRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("game_id")]
        public long GameId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("predicted_outcome")]
        public int PredictedOutcome { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("inzet")]
        public int Inzet { get; set; }
    }
}
