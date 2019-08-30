using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using EchoBot1.CognitiveModels;
using System.Linq;
using System.Globalization;
using Microsoft.Bot.Schema;
using AdaptiveCards;

namespace EchoBot1.Dialogs
{
    public class BookingDialog : ComponentDialog
    {
        private const string BookingInfo = "value-userInfo";
        private readonly BookingRecognizer _luisRecognizer;
        public BookingDialog(BookingRecognizer luisRecognizer) : base(nameof(BookingDialog))
        {
            _luisRecognizer = luisRecognizer;
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt),TimePromptValidator));
            AddDialog(new DateTimePrompt(nameof(DateTimePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DateAsync,
                AssetRoomAsync,
                TimeFromAsync,
                TimeToAsync,
                SummaryStepAsync,
                FinalAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> DateAsync(WaterfallStepContext stepContext,CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingModel)stepContext.Options;
            if (bookingDetails.Date == null)
            {
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("What date you want to book ?"),
                        RetryPrompt = MessageFactory.Text("Format is'n correct, Please enter again.")
                    },cancellationToken);
            }

            //await stepContext.Context.SendActivityAsync($"Your booking date is {bookingDetails.Date}");
            return await stepContext.NextAsync(bookingDetails.Date, cancellationToken);
        }
        private async Task<DialogTurnResult> AssetRoomAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingModel)stepContext.Options;
            if (bookingDetails.Date == null)
            {
                var stringDate = ((List<DateTimeResolution>)stepContext.Result)[0].Value;
                DateTime parsingDate = DateTime.Parse(stringDate);
                bookingDetails.Date = parsingDate.ToString("d MMMM yyyy");
                await stepContext.Context.SendActivityAsync("Thank !");
            }
            else
            {
                bookingDetails.Date = (string)stepContext.Result;
            }
            if (bookingDetails.AssetName == null)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Plese select room for booking."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Meeting Room 1", "Meeting Room 2", "Meeting Room 3" }),
                }, cancellationToken);
            }
            //await stepContext.Context.SendActivityAsync($"Your required room is {bookingDetails.AssetName}");
            return await stepContext.NextAsync(bookingDetails.AssetName, cancellationToken);
        }
        private async Task<DialogTurnResult> TimeFromAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingModel)stepContext.Options;
            if(bookingDetails.AssetName == null)
            {
                bookingDetails.AssetName = ((FoundChoice)stepContext.Result).Value;
                await stepContext.Context.SendActivityAsync("awesome !");
            }
            else
            {
                bookingDetails.AssetName = (string)stepContext.Result;
            }
            if (bookingDetails.TimeFrom == null)
            {
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What time you want to start ?"),
                    RetryPrompt = MessageFactory.Text("Format is'n correct, Please enter again.")
                }, cancellationToken);
            }
            //await stepContext.Context.SendActivityAsync($"Your required start time is {bookingDetails.TimeFrom}");
            return await stepContext.NextAsync(bookingDetails.TimeFrom, cancellationToken);
        }

        private async Task<DialogTurnResult> TimeToAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingModel)stepContext.Options;
            if (bookingDetails.TimeFrom == null)
            {
                var stringDate = ((List<DateTimeResolution>)stepContext.Result)[0].Value;
                DateTime parsingDate = DateTime.Parse(stringDate);
                bookingDetails.TimeFrom = parsingDate.ToString("H:mm");
            }
            if (bookingDetails.TimeTo == null)
            {
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("What time you want to finish ?"),
                        RetryPrompt = MessageFactory.Text("Format is'n correct, Please enter again.")
                    }, cancellationToken);
            }
            //await stepContext.Context.SendActivityAsync($"Your required end time is {bookingDetails.TimeTo}");
            return await stepContext.NextAsync(bookingDetails.TimeTo, cancellationToken);
        }
        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingModel)stepContext.Options;
            if (bookingDetails.TimeTo == null)
            {
                var stringDate = ((List<DateTimeResolution>)stepContext.Result)[0].Value;
                DateTime parsingDate = DateTime.Parse(stringDate);
                bookingDetails.TimeTo = parsingDate.ToString("H:mm");
            }
            var attachment = CreateAdaptiveCard(bookingDetails);
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions {
                    Prompt = MessageFactory.Text("Do you want to book ?")
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingModel)stepContext.Options;
            if ((bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync("booked success");
                return await stepContext.EndDialogAsync(null,cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Okay, I will not save this booking request");
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
        private async Task<bool> TimePromptValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync("Please enter the valid time",
                    cancellationToken: cancellationToken);
                return false;
            }
            var time = promptContext.Recognized.Value;
            var luisResult = await _luisRecognizer.RecognizeAsync<Booking>(promptContext.Context, cancellationToken);
            if (!string.IsNullOrEmpty(luisResult.BookTime()))
            {
                var timex = luisResult.BookTime();
                var resolution = TimexResolver.Resolve(new[] { timex }, DateTime.Today);
                if (luisResult.Entities.datetime.FirstOrDefault().Type == "time")
                {
                    var val = resolution.Values.ToArray();
                    time = val[0].Value;
                }
            }
            TimeSpan dummyOutput;
            if (!TimeSpan.TryParse(time, out dummyOutput))
            {
                await promptContext.Context.SendActivityAsync($"Your time format isn't correct !",
                    cancellationToken: cancellationToken);
                return false;
            }
            return true;
        }
        public Attachment CreateAdaptiveCard(BookingModel bookingDetail)
        {
            AdaptiveCard card = new AdaptiveCard();
            card.Speak = "Hello World";
            card.Body.Add(new TextBlock() {
                Text = "Booking Request",
                Size = TextSize.Large,
                Weight = TextWeight.Bolder
            });
            card.Body.Add(new TextBlock()
            {
                Text = bookingDetail.Date
            });
            card.Body.Add(new TextBlock()
            {
                Text = bookingDetail.AssetName
            });
            card.Body.Add(new TextBlock()
            {
                Text = $"{bookingDetail.TimeFrom} -  {bookingDetail.TimeTo}"
            });
            //card.Actions.Add(new SubmitAction()
            //{
            //    Title = "Contact",
            //    Speak = "<s>Contact</s>",

            //});
        Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachment;
        }

    }
}
