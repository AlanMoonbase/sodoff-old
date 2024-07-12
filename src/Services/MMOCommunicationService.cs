using Microsoft.Extensions.Options;
using sodoff.Configuration;
using sodoff.Model;
using System.Text;
using System.Text.Json;

namespace sodoff.Services
{
    public class MMOCommunicationService
    {
        public readonly DBContext DBContext;
        public readonly HttpClient httpClient;
        public readonly IOptions<ApiServerConfig> config;
        public MMOCommunicationService(DBContext dbContext, IOptions<ApiServerConfig> options)
        {
            DBContext = dbContext;
            config = options;
            httpClient = new HttpClient();
        }

        public bool SendPacketToRoom(string apiToken, string roomName, string cmd, string[] args)
        {
            var serializedArgs = JsonSerializer.Serialize(args);

            FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "apiToken", apiToken },
                { "roomName", roomName },
                { "cmd", cmd },
                { "serializedArgs", serializedArgs }
            });

            var result = httpClient.PostAsync($"http://{config.Value.MMOAdress}:{config.Value.MMOHttpApiPort}/mmo/update/SendPacketToRoom", form)?.Result;
            if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK) return true;
            else return false;
        }

        public bool SendPacketToPlayer(string apiToken, string userId, string cmd, string[] args)
        {
            var argsSerialized = JsonSerializer.Serialize(args);

            FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "apiToken", apiToken },
                { "userId", userId },
                { "cmd", cmd },
                { "serializedArgs", argsSerialized }
            });

            var result = httpClient.PostAsync($"http://{config.Value.MMOAdress}:{config.Value.MMOHttpApiPort}/mmo/update/SendPacketToPlayer", form)?.Result;
            if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK) return true;
            else return false;
        }

        public bool UpdateRoomVarsInRoom<T>(string apiToken, string roomName, Dictionary<string, T> vars)
        {
            var varsSerialized = JsonSerializer.Serialize(vars);

            FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "apiToken", apiToken },
                { "roomName", roomName },
                { "serializedVars", varsSerialized }
            });

            var result = httpClient.PostAsync($"http://{config.Value.MMOAdress}:{config.Value.MMOHttpApiPort}/mmo/update/UpdateRoomVarsInRoom", form)?.Result;
            if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK) return true;
            else return false;
        }
    }
}
