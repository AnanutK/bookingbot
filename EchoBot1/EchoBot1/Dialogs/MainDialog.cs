using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using EchoBot1.CognitiveModels;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Linq;
using System;

namespace EchoBot1.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        const int MaxError = 3;
        private readonly UserState _userState;
        private readonly BookingRecognizer _luisRecognizer;
        private int errorTimes = 0;

        public MainDialog(UserState userState, BookingRecognizer luisRecognizer) : base(nameof(MainDialog))
        {
            _userState = userState;
            _luisRecognizer = luisRecognizer;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new BookingDialog(luisRecognizer));
            AddDialog(new CancelDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                MenuChoice,
                InitialStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> MenuChoice(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            if (stepContext.Options?.ToString() == "success"){
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Chat with me again if you need help."),
                    }, cancellationToken);
            }
            else if (stepContext.Options?.ToString() == "greet")
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {}, cancellationToken);
            }
            var messageText = string.IsNullOrEmpty(stepContext.Options?.ToString())? "Or select a topic below.": "Or you can use a suggestion below";
            var reply = stepContext.Context.Activity.CreateReply(messageText);
            if (string.IsNullOrEmpty(stepContext.Options?.ToString()))
            {
                reply.Type = ActivityTypes.Message;
                reply.TextFormat = TextFormatTypes.Plain;
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                    {
                        new CardAction(){ Title = "Book a meeting room", Type=ActionTypes.ImBack, Value="booking" },
                        new CardAction(){ Title = "booking cancel", Type=ActionTypes.ImBack, Value="cancel" }
                    }
                };
            }
            else
            {
                var firstAction = stepContext.Options?.ToString();
                var secondAction = CheckIsMaxError();
                reply.Type = ActivityTypes.Message;
                reply.TextFormat = TextFormatTypes.Plain;
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                    {
                        new CardAction(){ Title = firstAction, Type=ActionTypes.ImBack, Value=firstAction }
                    }
                };
                if(errorTimes >= MaxError)
                {
                    reply.SuggestedActions.Actions.Add(new CardAction() { Title = secondAction, Type = ActionTypes.ImBack, Value = secondAction });
                }
            }
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = reply }, cancellationToken);
        }
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            var luisResult = await _luisRecognizer.RecognizeAsync<Booking>(stepContext.Context, cancellationToken);
            double intentscore = (luisResult != null) ? luisResult.TopIntent().score : 0.0;
            if (intentscore >= 0.80)
            {
                switch (luisResult.TopIntent().intent)
                {
                    case Booking.Intent.booking:
                        var bookingDetail = new BookingModel();
                        if (!string.IsNullOrEmpty(luisResult.BookTime()))
                        {
                            var timex = luisResult.BookTime();
                            var resolution = TimexResolver.Resolve(new[] { timex }, DateTime.Today);
                            var timexProperty = new TimexProperty(timex);
                            var val = resolution.Values.ToArray();
                            var timeType = val[0].Type;
                            if (timeType == "timerange")
                            {
                                DateTime startTime = DateTime.Parse(val[0].Start);
                                DateTime endTime = DateTime.Parse(val[0].End);
                                bookingDetail.TimeFrom = startTime.ToString("H:mm");
                                bookingDetail.TimeTo = endTime.ToString("H:mm");
                            }
                            else if (timeType == "time")
                            {
                                DateTime startTime = DateTime.Parse(val[0].Value);
                                bookingDetail.TimeFrom = startTime.ToString("H:mm");
                            }
                            else if (timeType == "date")
                            {
                                var t = val[0].Value;
                                DateTime parsingDate = DateTime.Parse(val[0].Value);
                                bookingDetail.Date = parsingDate.ToString("d MMMM yyyy");
                            }
                            else if (timeType == "datetime")
                            {
                                var t = val[0].Value;
                                DateTime parsingDate = timex == "PRESENT_REF" ? DateTime.Now : DateTime.Parse(val[0].Value);
                                bookingDetail.Date = parsingDate.ToString("d MMMM yyyy");
                                bookingDetail.TimeFrom = parsingDate.ToString("H:mm");
                                
                            }
                            else if (timeType == "daterange")
                            {
                                var t = val[0].Start;
                                DateTime parsingDate = DateTime.Parse(val[0].Start);
                                bookingDetail.Date = parsingDate.ToString("d MMMM yyyy");
                            }
                            else if (timeType == "datetimerange")
                            {
                                var t = val[0].Start;
                                var t2 = val[0].End;
                                DateTime parsingDateStart = DateTime.Parse(val[0].Start);
                                DateTime parsingDateEnd = DateTime.Parse(val[0].End);
                                bookingDetail.Date = parsingDateStart.ToString("d MMMM yyyy");
                                bookingDetail.TimeFrom = parsingDateStart.ToString("H:mm");
                                var startDate = parsingDateStart.Date;
                                var endDate = parsingDateEnd.Date;
                                if (DateTime.Compare(startDate, endDate) == 0)
                                {
                                    bookingDetail.TimeTo = parsingDateEnd.ToString("H:mm");
                                }
                            }
                            
                        }
                        bookingDetail.AssetName = luisResult.RoomEntities();
                        return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetail, cancellationToken);

                    case Booking.Intent.greeting:
                        await stepContext.Context.SendActivityAsync("Hi , I'm chat bot");
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, "greet", cancellationToken);

                    case Booking.Intent.cancel:
                        return await stepContext.BeginDialogAsync(nameof(CancelDialog), null, cancellationToken);

                    default:
                        // Catch all for unhandled intents
                        var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way.";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        errorTimes++;
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, "booking", cancellationToken);
                }
            }
            else
            {
                var message = "Sorry, I don't understand your question. Please try asking in a different way ";
                var topIntent = luisResult.TopIntent().intent.ToString();
                await stepContext.Context.SendActivityAsync(message);
                errorTimes++;
                return await stepContext.ReplaceDialogAsync(InitialDialogId, topIntent, cancellationToken);
            }
        }
        //private async Task<DialogTurnResult> ContinueDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    if ((FoundChoice)stepContext.Result != null)
        //    {
        //        if (((FoundChoice)stepContext.Result).Value == "Booking room")
        //        {
        //            BookingModel bookingDetail = new BookingModel();
        //            return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetail, cancellationToken);
        //        }
        //        else if (((FoundChoice)stepContext.Result).Value == "Cancel room")
        //        {
        //            return await stepContext.BeginDialogAsync(nameof(CancelDialog), null, cancellationToken);
        //        }
        //        else
        //        {
        //            return await stepContext.BeginDialogAsync(nameof(MainDialog), null, cancellationToken);
        //        }
        //    }
        //    else
        //    {
        //        return await stepContext.NextAsync(null, cancellationToken);
        //    }
        //}
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(InitialDialogId,"success", cancellationToken);
        }
        private string CheckIsMaxError()
        {
            if(errorTimes >= MaxError)
            {
                return "Contact admin";
            }
            else
            {
                return "other";
            }
        }
    }
}
