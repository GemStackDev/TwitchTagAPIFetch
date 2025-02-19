using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetEnv;

namespace TwitchRequest
{
  class Program
  {
    static async Task Main(string[] args)
    {
      await HelixRequest.FetchLeaderboardData();
    }
  }
}
