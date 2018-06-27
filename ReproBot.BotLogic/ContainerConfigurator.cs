using System;
using System.Collections.Generic;
using System.Text;
using ReproBot.BotLogic.Helpers;
using Microsoft.Bot.Builder.Dialogs;
using Autofac;
using ReproBot.BotLogic.API.Managers.Interfaces;
using ReproBot.BotLogic.API.Managers;
using Microsoft.Bot.Builder.Azure;
using System.Reflection;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace ReproBot.BotLogic
{
    public class ContainerConfigurator
    {
        public static void RegisterContainerElements()
        {
            Conversation.UpdateContainer(builder =>
            {
                builder.RegisterType<RootLuisDialog>()
                    .InstancePerDependency();

                builder.RegisterType<ChatHelper>()
                        .As<IChatHelper>()
                        .SingleInstance();

                builder.RegisterType<CalculationApi>().
                        As<ICalculationApi>().
                        SingleInstance();

                builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                var store = new InMemoryDataStore();

                // Other storage options
                // var store = new TableBotDataStore("...DataStorageConnectionString..."); // requires Microsoft.BotBuilder.Azure Nuget package 
                // var store = new DocumentDbBotDataStore("cosmos db uri", "cosmos db key"); // requires Microsoft.BotBuilder.Azure Nuget package 

                builder.Register(c => store)
                    .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                    .AsSelf()
                    .SingleInstance();
            });
        }
    }
}
