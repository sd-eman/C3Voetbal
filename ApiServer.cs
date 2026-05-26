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

                // POST /api/bets
                else if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/api/bets")
                {
                    using var reader = new System.IO.StreamReader(request.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var bet = JsonSerializer.Deserialize<Bet>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    using var db = new AppDbContext();
                    db.Bets.Add(bet!);
                    db.SaveChanges();

                    response.StatusCode = 201;
                    responseJson = JsonSerializer.Serialize(bet);
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
}
