using ReproBot.BotLogic.API.Managers.Interfaces;
using ReproBot.BotLogic.Helpers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ReproBot.BotLogic
{
    //TODO: Fill these in
    [LuisModel("<appid>", "<secret>", Staging = true)]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private IChatHelper _chat;
        private readonly ICalculationApi _dllCalculationApi;
        private static readonly string ConnectionName = ConfigurationManager.AppSettings["OauthConnectionName"];

        public RootLuisDialog(IChatHelper chat, ICalculationApi dllCalculationApi)
        {
            _chat = chat;
            this._dllCalculationApi = dllCalculationApi;
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = string.Format(BotResponses.RootLuisDialog_None, result.Query);

            await _chat.PostAsync(context, message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Calculation_v2")]
        public async Task CalculateInterest(IDialogContext context, LuisResult result)
        {
            Trace.TraceInformation(DateTime.Now + " Starting finance calculation for " + context.Activity.ChannelId + " for converstation " + context.Activity.Conversation.Id);

            await _chat.PostAsync(context, BotResponses.RootLuisDialog_CalculateInterest);

            var calculation_v2Form = new Calculation_v2Form();
            var calculation_v2FormDialog = new FormDialog<Calculation_v2Form>(calculation_v2Form, Calculation_v2Form.BuildForm, FormOptions.PromptInStart, result.Entities, CultureInfo.InvariantCulture);

            Trace.TraceInformation(DateTime.Now + " Starting FormFlow");
            context.Call(calculation_v2FormDialog, ResumeAfterCalculation_v2FormDialog);
        }

        [LuisIntent("SignIn")]
        public async Task SignIn(IDialogContext context, LuisResult result)
        {
            Trace.TraceInformation(DateTime.Now + " Sign in in channel " + context.Activity.ChannelId + " for conversation " + context.Activity.Conversation.Id);

            var entities = result.Entities;
            await context.Forward(new SignInDialog(entities), ResumeAfterSignInDialog2, entities, CancellationToken.None);
        }

        [LuisIntent("SignOut")]
        public async Task SignOut(IDialogContext context, LuisResult result)
        {
            Trace.TraceInformation(DateTime.Now + " Sign out in channel " + context.Activity.ChannelId + " for conversation " + context.Activity.Conversation.Id);

            // Check if there is already a token for this user
            Activity activity = (Activity)context.Activity;
            var oauthClient = activity.GetOAuthClient();
            var token = await oauthClient.OAuthApi.GetUserTokenAsync(activity.From.Id, ConnectionName).ConfigureAwait(false);

            if (token != null)
            {
                var signedout = await oauthClient.OAuthApi.SignOutUserAsync(activity.From.Id, ConnectionName).ConfigureAwait(false);
                await _chat.PostAsync(context, "You have been signed out!");
            }
            else
            {
                await _chat.PostAsync(context, "I can't sign you out. You're not signed in");
            }
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("EndConversation")]
        public async Task EndConversation(IDialogContext context, LuisResult result)
        {
            Trace.TraceInformation(DateTime.Now + " Ending conversation for " + context.Activity.ChannelId + " for converstation " + context.Activity.Conversation.Id);
            await _chat.PostAsync(context, BotResponses.RootLuisDialog_EndConversation);
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await _chat.PostAsync(context, BotResponses.RootLuisDialog_Help);
            context.Wait(this.MessageReceived);
        }

        private async Task ResumeAfterSignInDialog2(IDialogContext context, IAwaitable<IList<EntityRecommendation>> result)
        {
            await _chat.PostAsync(context, "You have been signed in!");
            context.Done<object>(null);
        }

        private async Task ResumeAfterCalculation_v2FormDialog(IDialogContext context, IAwaitable<Calculation_v2Form> result)
        {
            try
            {
                await _chat.PostAsync(context, BotResponses.RootLuisDialog_ResumeAfterCalculation_Confirmation);

                var extractedCalculationForm = await result;

                var responseCalculation = await _dllCalculationApi.CallCalculations_v2Api(extractedCalculationForm);

                await _chat.PostAsync(context, string.Format(BotResponses.RootLuisDialog_ResumeAfterCalculation_Result, responseCalculation.PaymentAmount, extractedCalculationForm.SalePrice, extractedCalculationForm.PaymentFrequency.ToString().ToLower(), extractedCalculationForm.NumberOfMonths, extractedCalculationForm.InterestRate));
                await _chat.PostAsync(context, BotResponses.RootLuisDialog_ResumeAfterCalculation_OfferMore);
            }
            catch (FormCanceledException ex)
            {
                if (ex.InnerException == null)
                {
                    var reply = "You have canceled the operation.";
                    await _chat.PostAsync(context, reply);
                }
            }
            finally
            {
                context.Done<object>(null);
            }
        }
    }
}
