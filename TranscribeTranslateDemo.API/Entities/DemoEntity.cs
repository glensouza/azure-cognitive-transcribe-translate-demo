﻿using Azure;
using Azure.Data.Tables;

namespace TranscribeTranslateDemo.API.Entities;

public class DemoEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Demo";
    public string RowKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SourceAudioFileUrl { get; set; } = string.Empty;
    public string LanguageFrom { get; set; } = string.Empty;
    public string LanguageTo { get; set; } = string.Empty;
    public string Transcription { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string TranslatedAudioFileUrl { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
