using Azure;
using Azure.Data.Tables;

namespace TranscribeTranslateDemo.API;

public class DemoEntity : ITableEntity
{
    public string PartitionKey { get; set; }

    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Text { get; set; }
}
