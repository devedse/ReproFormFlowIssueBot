//using System;
//using System.Threading.Tasks;
//using ReproBot.BotLogic;
//using Microsoft.Bot.Builder.Dialogs;
//using Microsoft.Bot.Connector;

//namespace ReproBot.Bot.Dialogs
//{
//    [Serializable]
//    public class RootDialog : IDialog<object>
//    {
//        public Task StartAsync(IDialogContext context)
//        {
//            context.Wait(MessageReceivedAsync);

//            return Task.CompletedTask;
//        }

//        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
//        {
//            var activity = await result as Activity;

//            // calculate something for us to return
//            string response = await MainDialogBot.ProcessMessageFromBotProject(activity);

//            // return our reply to the user
//            await context.PostAsync($"You sent {activity.Text} and my response is: {response}");

//            context.Wait(MessageReceivedAsync);
//        }
//    }
//}