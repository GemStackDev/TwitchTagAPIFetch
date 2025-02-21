namespace TwitchRequest
{
  public class secrets
  {
    [JsonPropertyName("CLIENT_ID")]
    public string ClientId { get; set; }

    [JsonPropertyName("CLIENT_SECRET")]
    public string ClientSecret { get; set; }

    [JsonPropertyName("ACCESS_TOKEN")]
    public string AccessToken { get; set; }

    [JsonPropertyName("REFRESH_TOKEN")]
    public string RefreshToken { get; set; }
  }
}
