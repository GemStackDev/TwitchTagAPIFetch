namespace TwitchRequest
{
  public static class HelixRequest
  {
    private static readonly string secretsFilePath = "Secrets.json";
    private static readonly HttpClient client = new HttpClient();

    private static string _accessToken;
    private static string _refreshToken;
    private static string _clientId;
    private static string _clientSecret;

    static HelixRequest()
    {
      LoadSecretsFromJson();
    }

    public static async Task FetchLeaderboardData()
    {
      LoadSecretsFromJson();

      if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_refreshToken) || string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
      {
        Console.WriteLine("Error: Missing required secrets. Check secrets.json");
        return;
      }

      client.DefaultRequestHeaders.Clear();
      client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
      client.DefaultRequestHeaders.Add("Client-Id", _clientId);

      var response = await client.GetAsync("https://api.twitch.tv/helix/bits/leaderboard?count=5&period=all");

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

      var refreshResponse = await client.PostAsync(refreshUrl, null); // Initiate request
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
      if (json.RootElement.TryGetProperty("access_token", out var newAccessToken))
      {
        _accessToken = newAccessToken.GetString();
        Console.WriteLine("Token refreshed successfully.");
        // Update the secrets.json
        UpdateSecretsFile();
        return true;
      }

      Console.WriteLine("Failed to retrieve new access token.");
      return false;
    }

    private static void LoadSecretsFromJson()
    {
      if (!File.Exists(secretsFilePath))
      {
        Console.WriteLine($"Error: {secretsFilePath} not found.");
        return;
      }

      try
      {
        // Read the json file
        string jsonString = File.ReadAllText(secretsFilePath);

        // Deserialize to secrets class
        var secrets = JsonSerializer.Deserialize<secrets>(jsonString);

        // Assign fields
        _clientId = secrets?.ClientId;
        _clientSecret = secrets?.ClientSecret;
        _accessToken = secrets?.AccessToken;
        _refreshToken = secrets?.RefreshToken;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error reading {secretsFilePath}: {ex.Message}");
      }
    }

    private static void UpdateSecretsFile()
    {
      try
      {
        // Construct a new Secrets object with the updated values
        var secrets = new secrets
        {
          ClientId = _clientId,
          ClientSecret = _clientSecret,
          AccessToken = _accessToken,
          RefreshToken = _refreshToken,
        };

        // Serialize to JSON
        string newJson = JsonSerializer.Serialize(secrets, new JsonSerializerOptions { WriteIndented = true });

        // Overwrite the old secrets.json
        File.WriteAllText(secretsFilePath, newJson);

        Console.WriteLine($"Updated {secretsFilePath} with new tokens.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error updating {secretsFilePath}: {ex.Message}");
      }
    }
  }
}
