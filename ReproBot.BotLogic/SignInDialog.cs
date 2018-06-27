using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReproBot.BotLogic
{
    [Serializable]
    public class SignInDialog : IDialog<IList<EntityRecommendation>>
    {
        private readonly string ConnectionName = "REPROBOTOAUTH4JUNI";

        private int attempts = 3;
        public IList<EntityRecommendation> _entities { get; set; }

        public SignInDialog(IList<EntityRecommendation> entities)
        {
            this._entities = entities;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceived);
        }

        private async Task MessageReceived(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = (Activity)context.Activity;

            if (activity.Text == "quit")
            {
                context.Fail(new OperationCanceledException("You cancelled the operation."));
                return;
            }

            var oauthClient = activity.GetOAuthClient();
            var token = await oauthClient.OAuthApi.GetUserTokenAsync(activity.From.Id, ConnectionName).ConfigureAwait(false);
            if (token != null)
            {
                await context.PostAsync($"You are already signed in.");

                context.Done(this._entities);
            }
            else
            {
                // see if the user is pending a login, if so, try to take the validation code and exchange it for a token
                bool ActiveSignIn = false;
                if (context.UserData.ContainsKey("ActiveSignIn"))
                {
                    ActiveSignIn = context.UserData.GetValue<bool>("ActiveSignIn");
                }

                if (!ActiveSignIn)
                {
                    // If the bot is not waiting for the user to sign in, ask them to do so
                    await context.PostAsync("Hello! Let's get you signed in!");

                    // Send an OAuthCard to get the user signed in
                    var oauthReply = await activity.CreateOAuthReplyAsync(ConnectionName, "Please sign in", "Sign in").ConfigureAwait(false);
                    await context.PostAsync(oauthReply);

                    // Save some state saying an Active sign in is in progress for this user
                    context.UserData.SetValue<bool>("ActiveSignIn", true);
                }
                else
                {
                    await context.PostAsync("Let's see if that code works...");

                    // try to exchange the message text for a token
                    token = await oauthClient.OAuthApi.GetUserTokenAsync(activity.From.Id, ConnectionName, magicCode: activity.Text).ConfigureAwait(false);
                    if (token != null)
                    {
                        await context.PostAsync($"It worked! You are now signed in");

                        // The sign in is complete so set state to note that
                        context.UserData.SetValue<bool>("ActiveSignIn", true);
                        context.Done(this._entities);
                    }
                    else
                    {
                        attempts--;
                        if (attempts > 0)
                        {
                            await context.PostAsync($"Hmm, that code wasn't right.");
                            context.Wait(this.MessageReceived);
                        }
                        else
                        {
                            context.Fail(new TooManyAttemptsException("I think the code you're trying is wrong. Ask \"Sign in\" to try again from the start."));
                        }
                    }
                }
            }
        }
    }
}