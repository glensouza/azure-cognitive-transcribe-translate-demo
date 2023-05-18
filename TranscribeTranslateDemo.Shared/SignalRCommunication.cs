using Newtonsoft.Json;

namespace TranscribeTranslateDemo.Shared;

public class SignalRNotification
{
    [JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
    public string Target { get; set; } = string.Empty;

    [JsonProperty("userId", NullValueHandling = NullValueHandling.Ignore)]
    public string UserId { get; set; } = string.Empty;

    [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
    public string Record { get; set; } = string.Empty;
}
