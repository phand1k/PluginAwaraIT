using Microsoft.Xrm.Sdk;
using PluginAwaraIT.Mapping;
using PluginAwaraIT.Repositories.Implementations;
using PluginAwaraIT.Services.Implementations;
using System;

namespace PluginAwaraIT.Plugin
{
    public class CreateInterestPluginWithPattern : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                var repository = new Repository(service);
                var clientService = new ClientService(repository, tracingService);

                if (context.MessageName.ToLower() == "create")
                {
                    tracingService.Trace("Обработка создания интереса...");
                    if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity targetEntity)
                    {
                        clientService.ProcessInterest(targetEntity);
                    }
                }
                else if (context.MessageName.ToLower() == "update" && context.PostEntityImages.Contains("PostImage"))
                {
                    tracingService.Trace("Обработка обновления статуса интереса...");
                    var postImage = context.PostEntityImages["PostImage"];
                    clientService.ProcessInterestStatusUpdate(postImage);
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Ошибка в плагине: {ex.Message}");
                throw new InvalidPluginExecutionException($"Произошла ошибка: {ex.Message}", ex);
            }
        }
    }
}
