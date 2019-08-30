using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace EchoBot1.Dialogs
{
    public class CancelDialog : ComponentDialog
    {
        public CancelDialog( ) : base(nameof(CancelDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt), BookIdValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                BookIdAsync,
                ConfirmAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> BookIdAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Plese enter your booking id."),
                    RetryPrompt = MessageFactory.Text("The value length entered must be 6."),
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> ConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["bookId"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text("Do you want to cancel this booking ?")
                 }, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync($"booking id \"{stepContext.Values["bookId"]}\" was cancel.");
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Okay, I will not cancel this booking");
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
        private static Task<bool> BookIdValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // This condition is our validation rule. You can also change the value at this point.
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value.Length == 6);
        }

    }
}

