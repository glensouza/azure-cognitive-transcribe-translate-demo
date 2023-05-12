using Azure;
using Azure.Data.Tables;

namespace TranscribeTranslateDemo.API.Entities;

public class DemoEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string UserId { get; set; }
    public string AudioFileUrl { get; set; }
    public string LanguageFrom { get; set; }
    public string LanguageTo { get; set; }
    public string Transcription { get; set; }
    public string Sentiment { get; set; }
    public string Translation { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
