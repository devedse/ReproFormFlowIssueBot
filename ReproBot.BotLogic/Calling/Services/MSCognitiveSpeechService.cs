using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ReproBot.BotLogic.Calling.Services
{
    public class MicrosoftCognitiveSpeechService
    {
        private readonly string subscriptionKey;
        private readonly string speechRecognitionUri;

        public MicrosoftCognitiveSpeechService()
        {
            System.Diagnostics.Trace.TraceWarning("We zitten in de cognitive services");
            this.DefaultLocale = "en-US";
            this.subscriptionKey = ConfigurationManager.AppSettings["MicrosoftSpeechApiKey"];
            this.speechRecognitionUri = Uri.UnescapeDataString(ConfigurationManager.AppSettings["MicrosoftSpeechRecognitionUri"]);
            System.Diagnostics.Trace.TraceWarning($"Subscription Key: {this.subscriptionKey}");
        }

        public string DefaultLocale { get; set; }

        /// <summary>
        /// Gets text from an audio stream.
        /// </summary>
        /// <param name="audiostream"></param>
        /// <returns>Transcribed text. </returns>
        public async Task<string> GetTextFromAudioAsync(Stream audiostream)
        {
            var requestUri = this.speechRecognitionUri + Guid.NewGuid();

            using (var client = new HttpClient())
            {
                var token = Authentication.Instance.GetAccessToken();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                try
                {
                    using (var binaryContent = new ByteArrayContent(StreamToBytes(audiostream)))
                    {
                        binaryContent.Headers.TryAddWithoutValidation("content-type", "audio/wav; codec=\"audio/pcm\"; samplerate=16000");
                        var response = await client.PostAsync(requestUri, binaryContent);
                        var responseString = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(responseString);

                        if (data != null)
                        {
                            return data.header.name;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                }
                catch (Exception exp)
                {
                    System.Diagnostics.Trace.TraceWarning("We hebben een error 1");
                    Trace.TraceError(exp.ToString());
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Converts Stream into byte[].
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <returns>Output byte[]</returns>
        private static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
