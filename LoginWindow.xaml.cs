using Microsoft.UI.Xaml;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;

namespace C3Voetbal
{
    public sealed partial class LoginWindow : Window
    {
        private static readonly HttpClient _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/")
        };

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Vul e-mail en wachtwoord in.";
                return;
            }

            LoginButton.IsEnabled = false;
            ErrorText.Text = "Bezig met inloggen...";

            try
            {
                var response = await _client.PostAsJsonAsync("login", new { email, password });

                if (!response.IsSuccessStatusCode)
                {
                    ErrorText.Text = "Ongeldige inloggegevens.";
                    LoginButton.IsEnabled = true;
                    return;
                }

                // TIJDELIJK - debug
                var rawJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Login response: {rawJson}");

                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResult>(rawJson,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                System.Diagnostics.Debug.WriteLine($"User id: {result?.User?.Id}");


                // var result = await response.Content.ReadFromJsonAsync<LoginResult>(
                //     new System.Text.Json.JsonSerializerOptions
                //     {
                 //        PropertyNameCaseInsensitive = true
                //     }); //


                // Hoofdscherm openen
                var main = new MainWindow(result.User);
                main.Activate();
                this.Close();
            }
            catch
            {
                ErrorText.Text = "Kan geen verbinding maken met de server.";
                LoginButton.IsEnabled = true;
            }
        }
    }

    public class LoginResult
    {
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public ulong Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
        public ulong? TeamId { get; set; }
    }
}