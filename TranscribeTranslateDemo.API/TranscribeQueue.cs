using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.Shared;
using Xabe.FFmpeg.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace TranscribeTranslateDemo.API;

public class TranscribeQueue
{
    private readonly ILogger logger;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly NotificationQueueClient notificationQueueClient;
    private readonly SpeechTranslationConfig speechTranslationConfig;

    public TranscribeQueue(ILoggerFactory loggerFactory, IConfiguration configuration, TableClient tableClient, BlobContainerClient blobClient, NotificationQueueClient notificationQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<TranscribeQueue>();
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.notificationQueueClient = notificationQueueClient;
        this.speechTranslationConfig = SpeechTranslationConfig.FromSubscription(configuration.GetValue<string>("SpeechKey"), configuration.GetValue<string>("SpeechRegion"));
    }

    [Function("TranscribeQueue")]
    public async Task Run([QueueTrigger(NotificationTypes.Transcription, Connection = "AzureWebJobsStorage")] string rowKey)
    {
        this.logger.LogInformation($"C# Queue trigger function processed: {rowKey}");

        DemoEntity? demo = await this.tableClient.GetEntityAsync<DemoEntity>("Demo", rowKey);
        if (demo == null)
        {
            return;
        }

        BlobClient? blobClient = this.blobContainerClient.GetBlobClient($"{rowKey}.flac");
        if (blobClient == null)
        {
            return;
        }

        SignalRNotification notification = new()
        {
            Target = NotificationTypes.Transcription,
            Record = $"Transcription Started {rowKey}",
            UserId = demo.UserId
        };
        await this.notificationQueueClient.SendMessageAsync(notification);

        notification.Target = NotificationTypes.Translation;
        notification.Record = $"Translation Started {rowKey}";
        await this.notificationQueueClient.SendMessageAsync(notification);

        string filename = Path.ChangeExtension(Path.GetTempFileName(), FileExtensions.Mp3);
        string directoryName = Path.GetDirectoryName(filename)!;
        string outputPath = Path.Combine(directoryName, $"{rowKey}.flac");
        string synthPath = Path.Combine(directoryName, $"{rowKey}synth.mp3");
        await blobClient.DownloadToAsync(outputPath);

        this.speechTranslationConfig.SpeechRecognitionLanguage = demo.LanguageFrom;
        this.speechTranslationConfig.AddTargetLanguage(demo.LanguageTo);

        string spanishVoice = "es-US-AlonsoNeural";
        this.speechTranslationConfig.VoiceName = spanishVoice;

        List<string> recognizedSpeeches = new();
        List<string> translatedSpeeches = new();
        List<byte[]> translatedAudio = new();

        TaskCompletionSource<int> stopTranslation = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Creates a translation recognizer using file as audio input.
        // Replace with your own audio file name.
        using AudioConfig audioInput = AudioConfig.FromWavFileInput(outputPath);
        using TranslationRecognizer recognizer = new(this.speechTranslationConfig, audioInput);
        // Subscribes to events.
        recognizer.Recognizing += async (s, e) =>
        {
            notification.Target = NotificationTypes.Transcription;
            notification.Record = $"RECOGNIZING in '{demo.LanguageFrom}': Text ={ e.Result.Text}";
            await this.notificationQueueClient.SendMessageAsync(notification);

            notification.Target = NotificationTypes.Translation;
            foreach (KeyValuePair<string, string> element in e.Result.Translations)
            {
                notification.Record = $"TRANSLATING into '{element.Key}': {element.Value}";
                await this.notificationQueueClient.SendMessageAsync(notification);
            }
        };

        recognizer.Recognized += async (s, e) =>
        {
            string recognizedSpeech = string.Empty;
            string translatedSpeech = string.Empty;

            switch (e.Result.Reason)
            {
                case ResultReason.TranslatedSpeech:
                    recognizedSpeech = $"RECOGNIZED in '{demo.LanguageFrom}': Text={e.Result.Text}";
                    recognizedSpeeches.Add(e.Result.Text);
                    //recognizedSpeech = e.Result.Text;
                    foreach (KeyValuePair<string, string> element in e.Result.Translations)
                    {
                        translatedSpeech = $"TRANSLATED into '{element.Key}': {element.Value}";
                        //translatedSpeech = element.Value;
                        translatedSpeeches.Add(element.Value);
                    }

                    notification.Target = NotificationTypes.Transcription;
                    notification.Record = recognizedSpeech;
                    await this.notificationQueueClient.SendMessageAsync(notification);

                    notification.Target = NotificationTypes.Translation;
                    notification.Record = translatedSpeech;
                    await this.notificationQueueClient.SendMessageAsync(notification);

                    break;
                case ResultReason.RecognizedSpeech:
                    recognizedSpeech = $"RECOGNIZED: Text={e.Result.Text}";
                    recognizedSpeeches.Add(e.Result.Text);
                    //recognizedSpeech = e.Result.Text;
                    translatedSpeech = "Speech not translated.";
                    translatedSpeeches.Add(translatedSpeech);

                    notification.Target = NotificationTypes.Transcription;
                    notification.Record = recognizedSpeech;
                    await this.notificationQueueClient.SendMessageAsync(notification);

                    notification.Target = NotificationTypes.Translation;
                    notification.Record = translatedSpeech;
                    await this.notificationQueueClient.SendMessageAsync(notification);

                    notification.Target = NotificationTypes.TextToSpeech;
                    notification.Record = "Unable to create synthesized text-to-speech";
                    await this.notificationQueueClient.SendMessageAsync(notification);

                    break;
                case ResultReason.NoMatch:
                    recognizedSpeech = "Speech could not be recognized.";
                    translatedSpeech = "Speech not translated.";
                    if (recognizedSpeeches.Count == 0)
                    {
                        recognizedSpeeches.Add(recognizedSpeech);
                        translatedSpeeches.Add(translatedSpeech);
                    }

                    notification.Target = NotificationTypes.TextToSpeech;
                    notification.Record = "Unable to create synthesized text-to-speech";
                    await this.notificationQueueClient.SendMessageAsync(notification);
                    break;
            }
        };

        recognizer.Synthesizing += async (s, e) =>
        {
            byte[] audio = e.Result.GetAudio();
            string audioSize = audio.Length != 0
                ? $"AudioSize: {audio.Length}"
                : $"AudioSize: {audio.Length} (end of synthesis data)";

            if (audio.Length <= 0)
            {
                return;
            }

            translatedAudio.Add(audio);

            notification.Target = NotificationTypes.TextToSpeech;
            notification.Record = audioSize;
            await this.notificationQueueClient.SendMessageAsync(notification);
        };

        recognizer.Canceled += (s, e) =>
        {
            string canceledReason = e.Reason == CancellationReason.Error ? $"CANCELED: Reason={e.Reason} || ErrorCode={e.ErrorCode} || ErrorDetails={e.ErrorDetails}" : $"CANCELED: Reason={e.Reason}";
            stopTranslation.TrySetResult(0);

            notification.Record = canceledReason;
            //await this.notificationQueueClient.SendMessageAsync(notification);
        };

        recognizer.SpeechStartDetected += (s, e) =>
        {
            Console.WriteLine("\nSpeech start detected event.");
        };

        recognizer.SpeechEndDetected += (s, e) =>
        {
            Console.WriteLine("\nSpeech end detected event.");
        };

        recognizer.SessionStarted += async (s, e) =>
        {
            notification.Target = NotificationTypes.Transcription;
            notification.Record = $"Transcription started {rowKey}";
            await this.notificationQueueClient.SendMessageAsync(notification);

            notification.Target = NotificationTypes.Translation;
            notification.Record = $"Translation started {rowKey}";
            await this.notificationQueueClient.SendMessageAsync(notification);

            notification.Target = NotificationTypes.TextToSpeech;
            notification.Record = $"Text-to-speech started {rowKey}";
            await this.notificationQueueClient.SendMessageAsync(notification);
        };

        recognizer.SessionStopped += (s, e) =>
        {
            string sessionStopped = $"Transcription stopped recognition {rowKey}";
            stopTranslation.TrySetResult(0);

            notification.Record = sessionStopped;
            //await this.notificationQueueClient.SendMessageAsync(notification);
        };

        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
        Console.WriteLine("Start translation...");
        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

        // Waits for completion.
        // Use Task.WaitAny to keep the task rooted.
        Task.WaitAny(stopTranslation.Task);

        // Stops translation.
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        await Task.Delay(5000).ConfigureAwait(false);

        await using FileStream fileStream = new(synthPath, FileMode.Create);
        byte[] audio = new byte[translatedAudio.Sum(a => a.Length)];
        int offset = 0;
        foreach (byte[] tempAudio in translatedAudio)
        {
            Buffer.BlockCopy(tempAudio, 0, audio, offset, tempAudio.Length);
            offset += tempAudio.Length;
        }
        fileStream.Write(audio, 0, audio.Length);
        fileStream.Flush();
        fileStream.Close();

        BlobClient? synthBlobClient = this.blobContainerClient.GetBlobClient($"{rowKey}synth.mp3");
        if (synthBlobClient == null)
        {
            return;
        }

        try
        {
            await synthBlobClient.UploadAsync(synthPath);
        }
        catch (Exception)
        {
            // ignored
        }

        string uri = blobClient.Uri.AbsoluteUri;
        if (synthBlobClient.CanGenerateSasUri)
        {
            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = synthBlobClient.GetParentBlobContainerClient().Name,
                BlobName = synthBlobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = synthBlobClient.GenerateSasUri(sasBuilder);
            uri = sasUri.AbsoluteUri;
        }

        string recognizedSpeech = recognizedSpeeches.Aggregate((a, b) => $"{a} {b}");
        string translatedSpeech = translatedSpeeches.Aggregate((a, b) => $"{a} {b}");

        bool retry = true;
        while (retry)
        {
            retry = false;
            try
            {
                demo.Transcription = recognizedSpeech;
                demo.Translation = translatedSpeech;
                demo.TranslatedAudioFileUrl = uri;
                await this.tableClient.UpdateEntityAsync(demo, demo.ETag, TableUpdateMode.Replace);
            }
            catch (Exception)
            {
                demo = await this.tableClient.GetEntityAsync<DemoEntity>("Demo", rowKey);
                retry = true;
            }
        }

        notification.Target = NotificationTypes.Transcription;
        notification.Record = recognizedSpeech;
        await this.notificationQueueClient.SendMessageAsync(notification);

        notification.Target = NotificationTypes.Translation;
        notification.Record = translatedSpeech;
        await this.notificationQueueClient.SendMessageAsync(notification);

        notification.Target = NotificationTypes.TextToSpeech;
        notification.Record = uri;
        await this.notificationQueueClient.SendMessageAsync(notification);

        if (File.Exists(outputPath))
        {
            try
            {
                File.Delete(outputPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        if (File.Exists(synthPath))
        {
            try
            {
                File.Delete(synthPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
