using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Plugin
{
    public class PreventDuplicatePriceListPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Получение сервисов из контекста
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            // Проверка, что создается новая запись прайс-листа
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity && entity.LogicalName == "nk_nkpricelistcourses")
            {
                // Получение дат начала и окончания из создаваемой записи
                DateTime? newStartDate = entity.Contains("nk_startdate") ? (DateTime?)entity["nk_startdate"] : null;
                DateTime? newEndDate = entity.Contains("nk_enddate") ? (DateTime?)entity["nk_enddate"] : null;

                if (newStartDate.HasValue && newEndDate.HasValue)
                {
                    // Преобразование дат в UTC
                    DateTime utcNewStartDate = newStartDate.Value.ToUniversalTime();
                    DateTime utcNewEndDate = newEndDate.Value.ToUniversalTime();

                    // Запрос существующих опубликованных прайс-листов с пересекающимися датами
                    var query = new QueryExpression("nk_nkpricelistcourses")
                    {
                        ColumnSet = new ColumnSet("nk_startdate", "nk_enddate"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
            {
                new ConditionExpression("statecode", ConditionOperator.Equal, 0), // Код статуса "Опубликован"
                new ConditionExpression("nk_startdate", ConditionOperator.LessEqual, utcNewEndDate),
                new ConditionExpression("nk_enddate", ConditionOperator.GreaterEqual, utcNewStartDate)
            }
                        }
                    };

                    var existingPriceLists = service.RetrieveMultiple(query);
                    if (existingPriceLists.Entities.Count > 0)
                    {
                        tracingService.Trace($"Найдено {existingPriceLists.Entities.Count} пересекающихся прайс-листов.");
                        foreach (var priceList in existingPriceLists.Entities)
                        {
                            if (priceList.Id == entity.Id || priceList.Id == Guid.Empty)
                            {
                                tracingService.Trace("Пропускаем текущую запись, так как это сама создаваемая запись или запись без идентификатора.");
                                continue;
                            }


                            tracingService.Trace($"Прайс-лист: {priceList.Id}, nk_startdate: {priceList.GetAttributeValue<DateTime>("nk_startdate")}, nk_enddate: {priceList.GetAttributeValue<DateTime>("nk_enddate")}");
                        }

                        tracingService.Trace($"Дата начала нового прайс-листа: {utcNewStartDate}");
                        tracingService.Trace($"Дата окончания нового прайс-листа: {utcNewEndDate}");
                        throw new InvalidPluginExecutionException("Невозможно создать новый прайс-лист, так как существует действующий прайс-лист с пересекающимся периодом действия.");
                    }
                }

                else
                {
                    throw new InvalidPluginExecutionException("Поля 'Дата начала' и 'Дата окончания' должны быть заполнены.");
                }
            }
        }
    }
}
