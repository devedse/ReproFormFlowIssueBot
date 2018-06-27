using Autofac;
using ReproBot.BotLogic.Calling.Services;
using ReproBot.BotLogic.Helpers;
using Microsoft.Bot.Builder.Calling;
using Microsoft.Bot.Builder.Calling.Events;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ReproBot.BotLogic.Calling
{
    public class IVRBot : IDisposable, ICallingBot
    {
        private static string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static string botId = ConfigurationManager.AppSettings["BotId"];

        private readonly Dictionary<string, CallState> callStateMap = new Dictionary<string, CallState>();

        private readonly MicrosoftCognitiveSpeechService speechService = new MicrosoftCognitiveSpeechService();

        public IVRBot(ICallingBotService callingBotService)
        {
            if (callingBotService == null)
            {
                throw new ArgumentNullException(nameof(callingBotService));
            }

            this.CallingBotService = callingBotService;

            this.CallingBotService.OnIncomingCallReceived += this.OnIncomingCallReceived;
            this.CallingBotService.OnPlayPromptCompleted += this.OnPlayPromptCompleted;
            this.CallingBotService.OnRecordCompleted += this.OnRecordCompleted;
            this.CallingBotService.OnRecognizeCompleted += this.OnRecognizeCompleted;
            this.CallingBotService.OnHangupCompleted += OnHangupCompleted;
        }

        public ICallingBotService CallingBotService { get; }

        public void Dispose()
        {
            if (this.CallingBotService != null)
            {
                this.CallingBotService.OnIncomingCallReceived -= this.OnIncomingCallReceived;
                this.CallingBotService.OnPlayPromptCompleted -= this.OnPlayPromptCompleted;
                this.CallingBotService.OnRecordCompleted -= this.OnRecordCompleted;
                this.CallingBotService.OnRecognizeCompleted -= this.OnRecognizeCompleted;
                this.CallingBotService.OnHangupCompleted -= this.OnHangupCompleted;
            }
        }

        private Task OnHangupCompleted(HangupOutcomeEvent hangupOutcomeEvent)
        {
            System.Diagnostics.Trace.TraceInformation("We zitten in OnHangupCompleted" + hangupOutcomeEvent.ConversationResult.Id);
            hangupOutcomeEvent.ResultingWorkflow = null;

            var callState = this.callStateMap[hangupOutcomeEvent.ConversationResult.Id];
            var client = new DirectLineClient(directLineSecret);
            var thisParticipant = callState.Participants.Single(t => t.Originator);

            var endConversationActivity = new Activity()
            {
                Type = "EndConversation",
                From = new ChannelAccount()
                {
                    Id = thisParticipant.DisplayName
                }
            };

            client.Conversations.PostActivityAsync(callState.Conversation.ConversationId, endConversationActivity);

            this.callStateMap.Remove(hangupOutcomeEvent.ConversationResult.Id);

            return Task.FromResult(true);
        }

        private async Task OnIncomingCallReceived(IncomingCallEvent incomingCallEvent)
        {
            System.Diagnostics.Trace.TraceInformation(DateTime.Now + " CallReceived " + incomingCallEvent.IncomingCall.Id);

            var client = new DirectLineClient(directLineSecret);

            var conversation = client.Conversations.StartConversation();
            string watermark = null;
            var callState = new CallState(incomingCallEvent.IncomingCall.Participants, conversation, watermark);

            await Task.Delay(1000);

            List<ActionBase> actionList = await GetActionListOnIncomingCall(client, callState);

            incomingCallEvent.ResultingWorkflow.Actions = actionList;
            this.callStateMap[incomingCallEvent.IncomingCall.Id] = callState;

            //return Task.FromResult(true);
        }

        private async Task<List<ActionBase>> GetActionListOnIncomingCall(DirectLineClient client, CallState callState)
        {
            var actionList = new List<ActionBase>();
            actionList.Add(new Answer { OperationId = Guid.NewGuid().ToString() });

            await GetActionListFromBotService(client, callState, actionList);
            return actionList;
        }

        private async Task GetActionListFromBotService(DirectLineClient client, CallState callState, List<ActionBase> actionList)
        {
            var result = await ReadBotMessagesAsync(client, callState);

            bool hangup = false;
            string promptForRecord = null;
            for (int i = 0; i < result.Count; ++i)
            {
                if (result[i] == BotResponses.RootLuisDialog_EndConversation)
                {
                    hangup = true;
                }
                if (i < result.Count - 1 || result[i] == BotResponses.RootLuisDialog_EndConversation)
                {
                    var speech = VoiceHelper.GetPromptForText(result[i]);
                    await SendSTTResultToUser(result[i], callState.Participants);
                    actionList.Add(speech);
                }
                else
                {
                    // This is the last element that we will use for the recording prompt
                    promptForRecord = result[i];
                }
            }

            if (hangup)
            {
                actionList.Add(new Hangup() { OperationId = Guid.NewGuid().ToString() });
            }
            else
            {
                actionList.Add(await GetRecordingAction(promptForRecord, callState));
            }
            //var record = GetRecordingAction(promptForRecord);

            //actionList.Add(record);
        }

        private async Task<List<ActionBase>> GetActionListOnRecordCompleted(DirectLineClient client, CallState callState)
        {
            List<ActionBase> actionList = new List<ActionBase>();
            await GetActionListFromBotService(client, callState, actionList);
            return actionList;
        }

        private Task OnPlayPromptCompleted(PlayPromptOutcomeEvent playPromptOutcomeEvent)
        {
            System.Diagnostics.Trace.TraceInformation("We zitten in OnPlayPromptCompleted" + playPromptOutcomeEvent.ConversationResult.Id);
            //var callState = this.callStateMap[playPromptOutcomeEvent.ConversationResult.Id];
            //SetupRecording(playPromptOutcomeEvent.ResultingWorkflow);
            return Task.FromResult(true);
        }

        private async Task OnRecordCompleted(RecordOutcomeEvent recordOutcomeEvent)
        {
            Trace.TraceInformation(DateTime.Now + " RecordCompleted " + recordOutcomeEvent.ConversationResult.Id);

            // Convert the audio to text
            if (recordOutcomeEvent.RecordOutcome.Outcome == Outcome.Success)
            {
                var text = await GetResultFromRecording(recordOutcomeEvent);
                var client = new DirectLineClient(directLineSecret);

                var callState = this.callStateMap[recordOutcomeEvent.ConversationResult.Id];

                Activity userMessage = GenerateActivity(text, callState);
                Trace.TraceInformation(DateTime.Now + " Posting message to message controller");
                // The result below contains the conversationId, as well as an incrementing number for each back and forth
                var resulttt = await client.Conversations.PostActivityAsync(callState.Conversation.ConversationId, userMessage);

                var actionList = await GetActionListOnRecordCompleted(client, callState);

                recordOutcomeEvent.ResultingWorkflow.Actions = actionList;
            }

            //recordOutcomeEvent.ResultingWorkflow.Links = null;
            //this.callStateMap.Remove(recordOutcomeEvent.ConversationResult.Id);
        }

        private async Task<string> GetResultFromRecording(RecordOutcomeEvent recordOutcomeEvent)
        {
            var record = await recordOutcomeEvent.RecordedContent;
            return await this.GetTextFromAudioAsync(record);
        }

        private static Activity GenerateActivity(string text, CallState callState)
        {
            var thisParticipant = callState.Participants.Single(t => t.Originator);
            return new Activity
            {
                ChannelId = "directline",
                Conversation = new ConversationAccount(id: callState.Conversation.ConversationId),
                From = new ChannelAccount(id: thisParticipant.Identity, name: thisParticipant.DisplayName),
                Id = callState.Conversation.ConversationId + "|" + (int.Parse(callState.Watermark) + 1).ToString("D7"),
                Locale = thisParticipant.LanguageId,
                Text = text,
                Type = ActivityTypes.Message,
                ServiceUrl = ConfigurationManager.AppSettings["Microsoft.Bot.Builder.Calling.CallbackUrl"]
            };
        }

        private async Task<Record> GetRecordingAction(string promptText, CallState callState)
        {
            var id = Guid.NewGuid().ToString();

            var prompt = VoiceHelper.GetPromptForText(promptText);
            await SendSTTResultToUser(promptText, callState.Participants);
            var record = new Record
            {
                OperationId = id,
                PlayPrompt = prompt,
                MaxDurationInSeconds = 60,
                InitialSilenceTimeoutInSeconds = 5,
                MaxSilenceTimeoutInSeconds = 4,
                PlayBeep = true,
                RecordingFormat = RecordingFormat.Wav,
                StopTones = new List<char> { '#' }
            };

            return record;
        }

        private async Task SendSTTResultToUser(string text, IEnumerable<Participant> participants)
        {
            var to = participants.Single(x => x.Originator);
            var from = participants.First(x => !x.Originator);
            Trace.TraceInformation(DateTime.Now + " Speak to user: " + text);
            //await AgentListener.Resume(to.Identity, to.DisplayName, from.Identity, from.DisplayName, to.Identity, text);
        }

        /// <summary>
        /// Gets text from an audio stream.
        /// </summary>
        /// <param name="audiostream"></param>
        /// <returns>Transcribed text. </returns>
        private async Task<string> GetTextFromAudioAsync(Stream audiostream)
        {
            System.Diagnostics.Trace.TraceInformation("Naar de speechservice");
            var text = await this.speechService.GetTextFromAudioAsync(audiostream);
            Debug.WriteLine(text);
            return text;
        }

        private Task OnRecognizeCompleted(RecognizeOutcomeEvent recognizeOutcomeEvent)
        {
            System.Diagnostics.Trace.TraceInformation("We zitten in OnRecognizeCompleted");
            var callState = this.callStateMap[recognizeOutcomeEvent.ConversationResult.Id];

            return Task.FromResult(true);
        }

        private async Task<List<string>> ReadBotMessagesAsync(DirectLineClient client, CallState callState)
        {
            var actions = new List<string>();

            System.Diagnostics.Trace.TraceInformation(DateTime.Now + " ReadBotMessageAsync");

            var activitySet = await client.Conversations.GetActivitiesAsync(callState.Conversation.ConversationId, callState.Watermark);
            if (activitySet != null && activitySet.Watermark != null)
            {
                callState.Watermark = activitySet.Watermark;
            }

            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;

            foreach (var activity in activities)
            {
                if (activity.Attachments != null && activity.Attachments.Count() != 0)
                {
                    foreach (var attachment in activity.Attachments)
                    {
                        if (attachment.ContentType == "application/vnd.microsoft.card.hero")
                        {
                            Trace.TraceInformation(attachment.Content.ToString());
                            var heroCard = JsonConvert.DeserializeObject<HeroCard>(attachment.Content.ToString());

                            if (heroCard != null)
                            {
                                actions.Add(heroCard.Text);
                                StringBuilder listOptions = new StringBuilder("You can choose from ");
                                for (int i = 0; i < heroCard.Buttons.Count; i++)
                                {
                                    if (i == heroCard.Buttons.Count - 1)
                                    {
                                        listOptions.Append("and " + heroCard.Buttons[i].Value);
                                    }
                                    else
                                    {
                                        listOptions.Append(heroCard.Buttons[i].Value + ", ");
                                    }
                                }

                                actions.Add(listOptions.ToString());
                            }
                        }
                    }
                }
                else
                {
                    actions.Add(activity.Text);
                }
            }
            Trace.TraceInformation(DateTime.Now + " Finished ReadBotMessageAsync");
            return actions;
        }

        private class CallState
        {
            public CallState(IEnumerable<Participant> participants, Microsoft.Bot.Connector.DirectLine.Conversation conversation, string watermark)
            {
                this.Participants = participants;
                this.Conversation = conversation;
                this.Watermark = watermark;
            }

            public IEnumerable<Participant> Participants { get; }

            public Microsoft.Bot.Connector.DirectLine.Conversation Conversation { get; set; }
            public string Watermark { get; set; }
        }
    }
}

