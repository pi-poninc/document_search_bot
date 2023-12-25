// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Chatbot.Bots;
using Chatbot.Dialogs;
using Chatbot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chatbot
{
    public class Startup
    {
        private IConfiguration _configuration;
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<Dialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, Bot<Dialog>>();

            // Services
            services.AddSingleton<ILogService>(provider =>
            {
                var cosmosDBOptions = _configuration.GetRequiredSection(nameof(CosmosDBOptions))
                .Get<CosmosDBOptions>()
                ?? throw new InvalidOperationException();

                var endpointUrl = cosmosDBOptions.Endpoint;
                var authKey = cosmosDBOptions.Key;
                var databaseId = cosmosDBOptions.DatabaseId;
                var messageContainerId = cosmosDBOptions.MessageContainerId;
                var feedbackContainerId =  cosmosDBOptions.FeedbackContainerId;

                var cosmosClient = new CosmosClient(endpointUrl, authKey);
                var database = cosmosClient.GetDatabase(databaseId);
                var cosmosLogContainer = database.GetContainer(messageContainerId);
                var cosmosFeedbackContainer = database.GetContainer(feedbackContainerId);

                return new LogService(cosmosLogContainer, cosmosFeedbackContainer, provider.GetService<ILogger<LogService>>());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
