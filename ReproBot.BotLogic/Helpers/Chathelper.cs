using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ReproBot.BotLogic.Helpers
{
    [Serializable]
    public class ChatHelper : IChatHelper
    {
        public ChatHelper()
        {
            Trace.TraceWarning("Creating new ChatHelper");
        }

        public async Task PostAsync(IDialogContext context, string message)
        {
            await context.PostAsync(message);
        }
    }
}
