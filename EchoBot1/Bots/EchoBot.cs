// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.5.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.AI.Luis;

namespace EchoBot1.Bots
{
    public class Echobot<T> : ActivityHandler where T : Dialog
    {
        protected  Dialog Dialog;
        protected  BotState ConversationState;
        protected  BotState UserState;
        protected  ILogger Logger;
        protected readonly Dialog Dialog2;
        protected readonly BotState ConversationState2;
        protected readonly BotState UserState2;
        protected readonly ILogger Logger2;

        public Echobot(ConversationState conversationState, UserState userState, T dialog, ILogger<Echobot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
        }
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);            
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);

        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text($"Hello");
            var messageText = "Say something like \"Book a meeting room 1 at 1-2 pm on next monday\"";
            await turnContext.SendActivityAsync(reply,cancellationToken);
            await turnContext.SendActivityAsync(messageText);
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
