using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ReproBot.BotLogic;
using ReproBot.BotLogic.Helpers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Diagnostics;
using System;

namespace ReproBot.Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            Trace.TraceInformation(DateTime.Now + " Message received: " + activity.Text + " from " + activity.ChannelId + " in conversation " + activity.Conversation.Id);
            if (activity.Type == ActivityTypes.Message || activity.Type == ActivityTypes.ConversationUpdate)
            {
                var mainDialogBot = new MainDialogBot();
                string responseMessage = await mainDialogBot.ProcessMessageFromBotProject(activity);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}