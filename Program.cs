﻿namespace TwitchRequest
{
  class Program
  {
    static async Task Main(string[] args)
    {
      await HelixRequest.FetchLeaderboardData();
    }
  }
}
