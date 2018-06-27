using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ReproBot.BotLogic.Helpers
{
    public static class VoiceHelper
    {
        public static PlayPrompt GetPromptForText(string text)
        {
            var prompt = new Prompt { Value = text, Voice = VoiceGender.Female, SayAs = SayAs.Cardinal};
            return new PlayPrompt { OperationId = Guid.NewGuid().ToString(), Prompts = new List<Prompt> { prompt } };
        }
    }
}
