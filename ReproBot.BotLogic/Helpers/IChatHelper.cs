using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;

namespace ReproBot.BotLogic.Helpers
{
    // exract the extension method to make it testable.
    public interface IChatHelper
    {
        Task PostAsync(IDialogContext context, string message);
    }
}
