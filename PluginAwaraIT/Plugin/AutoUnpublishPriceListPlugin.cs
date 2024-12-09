using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace PluginAwaraIT.Plugin
{
    public class AutoUnpublishPriceListPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Получение сервисов из контекста
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                // Проверка, что обновляется сущность прайс-листа
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity && entity.LogicalName == "nk_nkpricelistcourses")
                {
                    // Если обновляемая запись содержит поле nk_enddate
                    if (entity.Contains("nk_enddate"))
                    {
                        tracingService.Trace("Обновляется поле nk_enddate.");

                        // Получение даты окончания из сущности
                        DateTime? endDate = entity.GetAttributeValue<DateTime?>("nk_enddate");

                        if (endDate.HasValue)
                        {
                            tracingService.Trace($"Дата окончания: {endDate.Value}");

                            // Если дата окончания прошла
                            if (endDate.Value.ToUniversalTime() <= DateTime.UtcNow)
                            {
                                tracingService.Trace("Дата окончания прошла. Выполняется снятие с публикации.");

                                // Снять с публикации (перевести в состояние Черновик)
                                Entity updateEntity = new Entity("nk_nkpricelistcourses")
                                {
                                    Id = entity.Id
                                };

                                updateEntity["statecode"] = new OptionSetValue(0); // 0 = Черновик
                                updateEntity["statuscode"] = new OptionSetValue(1); // 1 = Статус Черновик

                                service.Update(updateEntity);
                                tracingService.Trace("Прайс-лист успешно переведен в состояние Черновик.");
                            }
                            else
                            {
                                tracingService.Trace("Дата окончания еще не наступила. Никаких действий не требуется.");
                            }
                        }
                        else
                        {
                            tracingService.Trace("Поле nk_enddate отсутствует или не заполнено.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("Поле nk_enddate не обновлялось.");
                    }
                }
                else
                {
                    tracingService.Trace("Плагин вызван не для сущности nk_nkpricelistcourses.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Ошибка: {ex.Message}");
                throw new InvalidPluginExecutionException("Произошла ошибка при автоматическом снятии публикации прайс-листа.", ex);
            }
        }
    }
}
