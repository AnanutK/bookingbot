// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.5.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

using EchoBot1.Bots;
using EchoBot1.Dialogs;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.Luis;

namespace EchoBot1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

           
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register LUIS recognizer
            services.AddSingleton<BookingRecognizer>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<MainDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, Echobot<MainDialog>>();

            //services.AddSingleton(sp =>
            //{
            //    var luisApp = new LuisApplication(
            //        applicationId: "2c2e34fe-1f7f-4e21-80cf-d2066704ddb2",
            //        endpointKey: "80ed89a057cc4966bf2dc1578f8b376a",
            //        endpoint: "https://westus.api.cognitive.microsoft.com/");
            //    var luisPredictionOptions = new LuisPredictionOptions
            //    {
            //        IncludeAllIntents = true,
            //    };
            //    return new LuisRecognizer(
            //        application: luisApp,
            //        predictionOptions: luisPredictionOptions,
            //        includeApiResults: true);
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
