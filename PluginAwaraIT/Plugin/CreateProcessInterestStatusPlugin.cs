using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PluginAwaraIT.Mapping;
using PluginAwaraIT.Repositories.Implementations;
using System;

namespace PluginAwaraIT.Plugin
{
    public class CreateProcessInterestStatusPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace("Проверка события обновления...");
                if (context.MessageName.ToLower() != "update" || !context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity targetEntity))
                {
                    tracingService.Trace("Событие не является 'Update' или отсутствует Target.");
                    return;
                }

                tracingService.Trace("Проверка сущности...");
                if (targetEntity.LogicalName != ColumnMap.Interest.EntityLogicalName)
                {
                    tracingService.Trace("Обрабатываемая сущность не является 'Интерес'.");
                    return;
                }

                tracingService.Trace("Проверка наличия PostImage...");
                if (!context.PostEntityImages.Contains("PostImage"))
                {
                    tracingService.Trace("PostImage отсутствует в контексте.");
                    return;
                }

                var postImage = context.PostEntityImages["PostImage"];
                tracingService.Trace($"PostImage успешно получен. Атрибуты: {string.Join(", ", postImage.Attributes.Keys)}");

                var statusCode = postImage.GetAttributeValue<OptionSetValue>(ColumnMap.Interest.StatusCode)?.Value;
                const int AgreementStatusCode = 2; // Код статуса "Согласие"
                tracingService.Trace($"Проверка статуса: {statusCode}");
                if (statusCode != AgreementStatusCode)
                {
                    tracingService.Trace("Статус не является 'Согласие'.");
                    return;
                }

                var territoryId = postImage.GetAttributeValue<EntityReference>("nk_countryid")?.Id;
                if (territoryId == null || territoryId == Guid.Empty)
                {
                    tracingService.Trace("Территория не указана или неверный ID.");
                    return;
                }

                var repository = new Repository(service);

                tracingService.Trace($"Получение пользователей для территории {territoryId}...");
                var userIds = repository.GetUsersByTerritory(territoryId.Value, tracingService);
                if (userIds == null || userIds.Count == 0)
                {
                    throw new InvalidPluginExecutionException("Не удалось найти доступных пользователей для территории.");
                }

                tracingService.Trace("Поиск наименее загруженного пользователя...");
                Guid leastLoadedUser = Guid.Empty;
                int leastLoad = int.MaxValue;

                foreach (var userId in userIds)
                {
                    int userLoad = repository.GetUserLoad(userId, tracingService);
                    if (userLoad < leastLoad)
                    {
                        leastLoad = userLoad;
                        leastLoadedUser = userId;
                    }
                }

                if (leastLoadedUser == Guid.Empty)
                {
                    throw new InvalidPluginExecutionException("Не удалось определить наименее загруженного пользователя.");
                }

                tracingService.Trace($"Назначен наименее загруженный пользователь: {leastLoadedUser}");

                tracingService.Trace("Создание 'Возможной сделки'...");

                var possibleDeal = new Entity("nk_nkpossibledeal")
                {
                    ["nk_nkclientid"] = postImage.GetAttributeValue<EntityReference>("nk_clientid"),
                    ["nk_nkstatuscode"] = new OptionSetValue(1),
                    ["nk_territoryid"] = new EntityReference("nk_nkcountries", territoryId.Value),
                    ["ownerid"] = new EntityReference("systemuser", leastLoadedUser),
                    ["nk_name"] = $"Возможная сделка №{Guid.NewGuid()}"
                };

                var possibleDealId = service.Create(possibleDeal);
                tracingService.Trace($"Возможная сделка создана с ID: {possibleDealId}");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Ошибка в плагине: {ex.Message}");
                throw new InvalidPluginExecutionException($"Произошла ошибка: {ex.Message}", ex);
            }
        }
    }
}