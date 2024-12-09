using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace PluginAwaraIT.Plugin
{
    public class ScheduleUnpublishPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity && entity.LogicalName == "nk_nkpricelistcourses")
                {
                    if (entity.Contains("nk_enddate"))
                    {
                        DateTime? endDate = entity.GetAttributeValue<DateTime?>("nk_enddate");
                        if (endDate.HasValue && endDate.Value > DateTime.UtcNow)
                        {
                            tracingService.Trace($"Запланировано снятие публикации для прайс-листа {entity.Id} на дату {endDate.Value}.");

                            // Создание асинхронного задания
                            Entity asyncOperation = new Entity("asyncoperation")
                            {
                                ["name"] = "Снятие публикации прайс-листа",
                                ["operationtype"] = 10, // Custom Plugin
                                ["postponeuntil"] = endDate.Value,
                                ["pluginassemblyid"] = context.PrimaryEntityId, // ID вашего плагина
                                ["statecode"] = 0, // Активное задание
                                ["statuscode"] = 1
                            };

                            service.Create(asyncOperation);
                        }
                    }
                    else
                    {
                        tracingService.Trace("Поле nk_enddate отсутствует.");
                    }
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Ошибка: {ex.Message}");
                throw new InvalidPluginExecutionException("Произошла ошибка при планировании снятия публикации.", ex);
            }
        }
    }
}
