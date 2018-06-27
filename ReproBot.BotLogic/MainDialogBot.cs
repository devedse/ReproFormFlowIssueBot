using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using ReproBot.BotLogic.Helpers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;

namespace ReproBot.BotLogic
{
    public class MainDialogBot
    {
        public async Task<string> ProcessMessageFromBotProject(Activity activity)
        {
            if (activity != null)
            {
                // one of these will have an interface and process it
                ConnectorClient client = new ConnectorClient(new Uri(activity.ServiceUrl));

                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        {
                            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                            {
                                await Conversation.SendAsync(activity, () => scope.Resolve<RootLuisDialog>());
                            }
                        }
                        break;
                    case ActivityTypes.ConversationUpdate:
                        if (activity.MembersAdded.Any(o => o.Id == activity.Recipient.Id))
                        {
                            var reply = activity.CreateReply();

                            reply.Text = "Hello, I'm ReproBot. How can I help you today?";

                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                        //var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                        //IConversationUpdateActivity update = activity;
                        //if (update.MembersAdded.Any())
                        //{
                        //    var reply = activity.CreateReply();
                        //    var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                        //    foreach (var newMember in newMembers)
                        //    {
                        //        reply.Text = "Hello, this is ReproBot. How can I help you today?";

                        //        await client.Conversations.ReplyToActivityAsync(reply);
                        //    }
                        //}
                        break;
                    case ActivityTypes.ContactRelationUpdate:
                    case ActivityTypes.Typing:
                    case ActivityTypes.DeleteUserData:
                    case ActivityTypes.Ping:
                    default:
                        //log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                        break;
                }
            }

            return "";
        }
    }
}
