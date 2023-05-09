using Newtonsoft.Json;

namespace TranscribeTranslateDemo.Shared;

public class SignalRNotification
{
    [JsonProperty("userId", NullValueHandling = NullValueHandling.Ignore)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the record.
    /// </summary>
    /// <value>The record.</value>
    [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
    public object Record { get; set; }
}
