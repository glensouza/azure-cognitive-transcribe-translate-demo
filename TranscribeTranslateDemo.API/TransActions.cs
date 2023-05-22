using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using TranscribeTranslateDemo.Shared;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;
using Xabe.FFmpeg.Streams;

namespace TranscribeTranslateDemo.API;

public class TransActions
{
    private readonly NotificationQueueClient notificationQueueClient;
    private readonly SpeechTranslationConfig speechTranslationConfig;

    public TransActions(NotificationQueueClient notificationQueueClient, SpeechTranslationConfig speechTranslationConfig)
    {
        this.notificationQueueClient = notificationQueueClient;
        this.speechTranslationConfig = speechTranslationConfig;
    }

    [FunctionName("TransActions")]
    public async Task Run(
        [QueueTrigger(NotificationTypes.Transcription, Connection = "AzureWebJobsStorage")] string rowKey,
        [Table(tableName: "Transcriptions", partitionKey: "Demo", rowKey: "{queueTrigger}", Connection = "AzureWebJobsStorage")] TranscriptionEntity transcription,
        [Table("Transcriptions", Connection = "AzureWebJobsStorage")] TableClient tableClient,
        [Blob("transcriptions/{queueTrigger}.mp3", FileAccess.Read)] Stream audioFileStream,
        [Blob("transcriptions")] BlobContainerClient blobContainerClient,
        ExecutionContext context,
        ILogger log)
    {
        if (string.IsNullOrEmpty(rowKey) || transcription == null || audioFileStream == null || audioFileStream.Length == 0)
        {
            return;
        }

        log.LogInformation($"C# Queue trigger function processed: {rowKey}");
        log.LogInformation($"PK={transcription.PartitionKey}, RK={transcription.RowKey}");
        log.LogInformation($"BlobInput processed blob\n Name: {rowKey}.mp3 \n Size: {audioFileStream.Length} bytes");

        SignalRNotification notification = new()
        {
            Target = NotificationTypes.Transcription,
            Record = $"Transcription Started {rowKey}",
            UserId = transcription.UserId
        };
        await this.notificationQueueClient.SendMessageAsync(notification);

        notification.Target = NotificationTypes.Translation;
        notification.Record = $"Translation Started {rowKey}";
        await this.notificationQueueClient.SendMessageAsync(notification);


        string outputPath = Path.ChangeExtension(Path.GetTempFileName(), FileExtensions.Mp3);
        //Debugger.Break();
        string directoryName = Path.GetDirectoryName(outputPath);
        string filePath = Path.Combine(directoryName, $"{rowKey}.mp3");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        try
        {
            SpamLog(log, "1");
            FFmpeg.ExecutablesPath = context.FunctionAppDirectory;

            await using (FileStream file = new(filePath, FileMode.Create, FileAccess.Write))
            {
                byte[] bytes = new byte[audioFileStream.Length];
                _ = await audioFileStream.ReadAsync(bytes.AsMemory(0, (int)audioFileStream.Length));
                file.Write(bytes, 0, bytes.Length);
                audioFileStream.Close();
            }

            SpamLog(log, "2");
            IMediaInfo inputFile = await MediaInfo.Get(filePath).ConfigureAwait(false);

            SpamLog(log, "2.1");
            IAudioStream audioStream = inputFile.AudioStreams.First();

            //Debugger.Break();
            SpamLog(log, "2.2");
            int sampleRate = audioStream.SampleRate;
            SpamLog(log, "2.3");
            int channels = audioStream.Channels;
            //CodecType codec = audioStream.CodecType;
            //Debugger.Break();

            if (sampleRate < 41100)
            {
                SpamLog(log, "2.4");
                audioStream.SetSampleRate(41100);
            }

            if (channels != 1)
            {
                SpamLog(log, "2.5");
                audioStream.SetChannels(1);
            }

            try
            {
                SpamLog(log, "2.6a");
                await Conversion.New().AddStream(audioStream).SetOutput(outputPath).Start().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                SpamLog(log, $"2.6b: {e.Message}");
                throw;
            }

            SpamLog(log, "2.7");
            await using Mp3FileReader mp3 = new(outputPath);
            SpamLog(log, "2.8");
            await using WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3);
            SpamLog(log, "2.9");
            WaveFileWriter.CreateWaveFile($"{outputPath}.flac", pcm);
            //WaveFileWriter.WriteWavFileToStream(outputStream, pcm);
            SpamLog(log, "3");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Debugger.Break();
        }















        this.speechTranslationConfig.SpeechRecognitionLanguage = transcription.LanguageFrom;
        this.speechTranslationConfig.AddTargetLanguage(transcription.LanguageTo);

        string spanishVoice = "es-US-AlonsoNeural";
        this.speechTranslationConfig.VoiceName = spanishVoice;

        List<string> recognizedSpeeches = new();
        List<string> translatedSpeeches = new();
        List<byte[]> translatedAudio = new();

        TaskCompletionSource<int> stopTranslation = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Creates a translation recognizer using file as audio input.
        //BinaryReader reader = new(audioFileStream);
        //BinaryReader reader = new(outputStream);
        //PushAudioInputStream audioInputStream = AudioInputStream.CreatePushStream();
        //using AudioConfig audioInput = AudioConfig.FromStreamInput(audioInputStream);
        //using AudioConfig audioInput = AudioConfig.FromStreamInput(audioFileStream);
        using AudioConfig audioInput = AudioConfig.FromWavFileInput($"{outputPath}.flac");
        using TranslationRecognizer recognizer = new(this.speechTranslationConfig, audioInput);
        SpamLog(log, "4");

        // Subscribes to events.
        recognizer.Recognizing += async (s, e) =>
        {
            SpamLog(log, $"5: {e.Result.Text}");
            notification.Target = NotificationTypes.Transcription;
            notification.Record = $"RECOGNIZING in '{transcription.LanguageFrom}': Text ={e.Result.Text}";
            await this.notificationQueueClient.SendMessageAsync(notification);

            notification.Target = NotificationTypes.Translation;
            foreach (KeyValuePair<string, string> element in e.Result.Translations)
            {
                SpamLog(log, $"6: {element.Value}");
                notification.Record = $"TRANSLATING into '{element.Key}': {element.Value}";
                await this.notificationQueueClient.SendMessageAsync(notification);
            }
        };

        recognizer.Recognized += async (s, e) =>
        {
            SpamLog(log, "7");

            string recognizedSpeech = string.Empty;
            string translatedSpeech = string.Empty;

            switch (e.Result.Reason)
            {
                case ResultReason.TranslatedSpeech:
                    recognizedSpeech = $"RECOGNIZED in '{transcription.LanguageFrom}': Text={e.Result.Text}";
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

                    SpamLog(log, $"8: {recognizedSpeech}");
                    SpamLog(log, $"9: {translatedSpeech}");
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

                    SpamLog(log, $"10: {recognizedSpeech}");
                    SpamLog(log, $"11: {translatedSpeech}");
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

                    SpamLog(log, $"12: {recognizedSpeech}");
                    SpamLog(log, $"13: {translatedSpeech}");
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

            SpamLog(log, $"14: {audioSize}");
        };

        recognizer.Canceled += (s, e) =>
        {
            string canceledReason = e.Reason == CancellationReason.Error ? $"CANCELED: Reason={e.Reason} || ErrorCode={e.ErrorCode} || ErrorDetails={e.ErrorDetails}" : $"CANCELED: Reason={e.Reason}";
            stopTranslation.TrySetResult(0);

            notification.Record = canceledReason;
            //await this.notificationQueueClient.SendMessageAsync(notification);

            SpamLog(log, $"15: {canceledReason}");
        };

        recognizer.SpeechStartDetected += (s, e) =>
        {
            //Console.WriteLine("\nSpeech start detected event.");
            SpamLog(log, "16: Speech start detected event.");
        };

        recognizer.SpeechEndDetected += (s, e) =>
        {
            //Console.WriteLine("\nSpeech end detected event.");
            SpamLog(log, "17: Speech end detected event.");
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

            SpamLog(log, "18: Session Started detected event.");
        };

        recognizer.SessionStopped += (s, e) =>
        {
            string sessionStopped = $"Transcription stopped recognition {rowKey}";
            stopTranslation.TrySetResult(0);

            notification.Record = sessionStopped;
            //await this.notificationQueueClient.SendMessageAsync(notification);

            SpamLog(log, "19: Session Stopped detected event.");
        };

        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
        //Console.WriteLine("Start translation...");
        SpamLog(log, "20: Start translation...");

        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

        // Waits for completion.
        // Use Task.WaitAny to keep the task rooted.
        Task.WaitAny(stopTranslation.Task);
        SpamLog(log, "21");
        
        // Stops translation.
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        SpamLog(log, "22");

        await Task.Delay(5000).ConfigureAwait(false);
        SpamLog(log, "23");

        //await using FileStream fileStream = new(synthPath, FileMode.Create);
        byte[] audio = new byte[translatedAudio.Sum(a => a.Length)];
        int offset = 0;
        foreach (byte[] tempAudio in translatedAudio)
        {
            Buffer.BlockCopy(tempAudio, 0, audio, offset, tempAudio.Length);
            offset += tempAudio.Length;
        }
        //fileStream.Write(audio, 0, audio.Length);
        //fileStream.Flush();
        //fileStream.Close();

        SpamLog(log, "24");
        BlobClient? synthBlobClient = blobContainerClient.GetBlobClient($"{rowKey}synth.mp3");
        //BlobClient? synthBlobClient = this.blobContainerClient.GetBlobClient($"{rowKey}synth.mp3");
        if (synthBlobClient == null)
        {
            return;
        }

        SpamLog(log, "25");
        bool fileExists = await synthBlobClient.ExistsAsync();
        while (fileExists)
        {
            await synthBlobClient.DeleteAsync();
            fileExists = await synthBlobClient.ExistsAsync();
        }

        try
        {
            await synthBlobClient.UploadAsync(new MemoryStream(audio));
        }
        catch (Exception)
        {
            // ignored
        }

        SpamLog(log, "26");
        string uri = synthBlobClient.Uri.AbsoluteUri;
        if (synthBlobClient.CanGenerateSasUri)
        {
            // Create a SAS token that's valid for one hour.
            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = synthBlobClient.GetParentBlobContainerClient().Name,
                BlobName = synthBlobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = synthBlobClient.GenerateSasUri(sasBuilder);
            log.LogInformation("SAS URI for blob is: {0}", sasUri);

            uri = sasUri.AbsoluteUri;
        }
        else
        {
            //return new ExceptionResult(new Exception("BlobClient must be authorized with Shared Key credentials to create a service SAS."), false);
        }

        SpamLog(log, "27");
        string recognizedSpeech = recognizedSpeeches.Aggregate((a, b) => $"{a} {b}");
        string translatedSpeech = translatedSpeeches.Aggregate((a, b) => $"{a} {b}");

        bool retry = true;
        while (retry)
        {
            retry = false;
            try
            {
                transcription.Transcription = recognizedSpeech;
                transcription.Translation = translatedSpeech;
                transcription.TranslatedAudioFileUrl = uri;
                await tableClient.UpdateEntityAsync(transcription, transcription.ETag, TableUpdateMode.Replace);
            }
            catch (Exception)
            {
                transcription = await tableClient.GetEntityAsync<TranscriptionEntity>("Demo", rowKey);
                retry = true;
            }
        }

        SpamLog(log, "28");
        notification.Target = NotificationTypes.Transcription;
        notification.Record = recognizedSpeech;
        await this.notificationQueueClient.SendMessageAsync(notification);

        notification.Target = NotificationTypes.Translation;
        notification.Record = translatedSpeech;
        await this.notificationQueueClient.SendMessageAsync(notification);

        notification.Target = NotificationTypes.TextToSpeech;
        notification.Record = uri;
        await this.notificationQueueClient.SendMessageAsync(notification);

        //if (File.Exists(outputPath))
        //{
        //    try
        //    {
        //        File.Delete(outputPath);
        //    }
        //    catch (Exception)
        //    {
        //        // ignored
        //    }
        //}

        //if (File.Exists(synthPath))
        //{
        //    try
        //    {
        //        File.Delete(synthPath);
        //    }
        //    catch (Exception)
        //    {
        //        // ignored
        //    }
        //}

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        if (File.Exists($"{outputPath}.flac"))
        {
            File.Delete($"{outputPath}.flac");
        }
    }

    private void SpamLog(ILogger log, string message)
    {
        log.LogInformation($"\n\n\n\n\n\n================================================\n\n{message}\n\n================================================\n\n\n\n\n");
    }
}
