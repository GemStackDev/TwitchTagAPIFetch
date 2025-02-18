using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetEnv;

class Program
{
    private static readonly string envFilePath = ".env";
    private static readonly HttpClient client = new HttpClient();

    private static string _accessToken;
    private static string _refreshToken;
    private static string _clientId;
    private static string _clientSecret;

    static async Task Main()
    {
        Env.Load(); // Load environment variables from .env file

        _accessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN");
        _refreshToken = Environment.GetEnvironmentVariable("REFRESH_TOKEN");
        _clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
        _clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

        if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_refreshToken) ||
            string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            Console.WriteLine("Error: Missing required environment variables.");
            return;
        }

        await FetchLeaderboardData();
    }

    private static async Task FetchLeaderboardData()
    {
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        HttpResponseMessage response = await client.GetAsync("https://api.twitch.tv/helix/bits/leaderboard?count=2&period=all");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("Token expired, attempting to refresh...");
            if (await RefreshAccessToken())
            {
                await FetchLeaderboardData();
            }
            else
            {
                Console.WriteLine("Failed to refresh token.");
            }
            return;
        }

        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseBody);
    }

    private static async Task<bool> RefreshAccessToken()
    {
        var refreshUrl = $"https://id.twitch.tv/oauth2/token?grant_type=refresh_token&refresh_token={_refreshToken}&client_id={_clientId}&client_secret={_clientSecret}";

        // Initiate the request
        HttpResponseMessage refreshResponse = await client.PostAsync(refreshUrl, null);
        // Check if the refresh response is not the success code and exit function with false
        if (!refreshResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to refresh token: {refreshResponse.StatusCode}");
            return false;
        }

        // Read the response as a string
        string responseContent = await refreshResponse.Content.ReadAsStringAsync();
        // Parse string into Json format
        using JsonDocument json = JsonDocument.Parse(responseContent);
        if (json.RootElement.TryGetProperty("access_token", out JsonElement newAccessToken))
        {
            _accessToken = newAccessToken.GetString();
            Console.WriteLine("Token refreshed successfully.");
            // Update the env file function
            UpdateEnvFile("ACCESS_TOKEN", _accessToken);
            return true;
        }

        Console.WriteLine("Failed to retrieve new access token.");
        return false;
    }

    private static void UpdateEnvFile(string key, string newValue)
    {
        if (!File.Exists(envFilePath))
        {
            Console.WriteLine(".env file not found!");
            return;
        }

        // Read all lines from the .env file
        string[] lines = File.ReadAllLines(envFilePath);
        bool keyFound = false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith($"{key}="))  // Find the key in the .env file
            {
                lines[i] = $"{key}={newValue}";  // Update its value
                keyFound = true;
                break;
            }
        }

        // If key was not found, throw error response
        if (!keyFound)
        {
            Console.WriteLine("Failed to update access token in .env");
        }
        else
        {
            File.WriteAllLines(envFilePath, lines); // Write updated lines back to the file
        }

        Console.WriteLine($".env updated: {key}={newValue}");
    }
}
