using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using PluginAwaraIT.Mapping;
using PluginAwaraIT.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Repositories.Implementations
{
    public class Repository : IRepository
    {
        private readonly IOrganizationService _service;

        public Repository(IOrganizationService service)
        {
            _service = service;
        }
        public int GetUserLoadForCallCenter(Guid userId, ITracingService tracingService)
        {
            tracingService.Trace($"Получение загрузки для пользователя с ID: {userId}");

            var query = new QueryExpression("nk_nkinterest")
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ownerid", ConditionOperator.Equal, userId),
                        new ConditionExpression("nk_statuscode", ConditionOperator.Equal, 1) // Статус "В работе"
                    }
                }
            };

            tracingService.Trace("Выполнение запроса для получения загрузки пользователя...");
            var result = _service.RetrieveMultiple(query);
            var loadCount = result.Entities.Count;

            tracingService.Trace($"Пользователь {userId} имеет загрузку: {loadCount}");
            return loadCount;
        }

        public int GetUserLoad(Guid userId, ITracingService tracingService)
        {
            tracingService.Trace($"Получение загрузки для пользователя с ID: {userId}");

            var query = new QueryExpression("nk_nkpossibledeal")
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ownerid", ConditionOperator.Equal, userId),
                        new ConditionExpression("nk_nkstatuscode", ConditionOperator.Equal, 1) // Статус "В работе"
                    }
                }
            };

            tracingService.Trace("Выполнение запроса для получения загрузки пользователя...");
            var result = _service.RetrieveMultiple(query);
            var loadCount = result.Entities.Count;

            tracingService.Trace($"Пользователь {userId} имеет загрузку: {loadCount}");
            return loadCount;
        }

        public Entity FindClientCard(string email, string phone)
        {
            var query = new QueryExpression(ColumnMap.ClientCard.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(ColumnMap.ClientCard.Id),
                Criteria =
            {
                Filters =
                {
                    new FilterExpression
                    {
                        FilterOperator = LogicalOperator.Or,
                        Conditions =
                        {
                            new ConditionExpression(ColumnMap.ClientCard.Email, ConditionOperator.Equal, email),
                            new ConditionExpression(ColumnMap.ClientCard.Phone, ConditionOperator.Equal, phone)
                        }
                    }
                }
            }
            };

            var results = _service.RetrieveMultiple(query);
            return results.Entities.FirstOrDefault();
        }

        public EntityReference FindLeastLoadedUserForCallCenter(Guid teamId, ITracingService tracingService)
        {
            tracingService.Trace($"Начало вызова FindLeastLoadedUser для команды с ID: {teamId}");

            // Получаем всех пользователей в команде
            var userQuery = new QueryExpression("teammembership")
            {
                ColumnSet = new ColumnSet("systemuserid"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("teamid", ConditionOperator.Equal, teamId)
            }
                }
            };

            tracingService.Trace("Выполнение запроса для получения пользователей команды...");
            var userResults = _service.RetrieveMultiple(userQuery);

            if (userResults.Entities.Count == 0)
            {
                tracingService.Trace("Для данной команды не найдено пользователей.");
                return null;
            }

            tracingService.Trace($"Найдено {userResults.Entities.Count} пользователей в команде {teamId}.");

            // Словарь для хранения загрузки пользователей
            var userLoads = new Dictionary<Guid, int>();

            foreach (var user in userResults.Entities)
            {
                var userId = user.GetAttributeValue<Guid>("systemuserid");
                var userLoad = GetUserLoadForCallCenter(userId, tracingService);
                userLoads[userId] = userLoad;

                tracingService.Trace($"Пользователь {userId} имеет загрузку: {userLoad}");
            }

            // Сортируем по загрузке, а затем по GUID
            var leastLoadedUserId = userLoads.OrderBy(u => u.Value).ThenBy(u => u.Key).First().Key;

            tracingService.Trace($"Выбран пользователь с ID: {leastLoadedUserId}.");
            return new EntityReference("systemuser", leastLoadedUserId);
        }


        public Guid CreateClientCard(Entity clientCard)
        {
            return _service.Create(clientCard);
        }

        public EntityReference FindLeastLoadedUser(Guid teamId, ITracingService tracingService)
        {
            tracingService.Trace($"Начало вызова FindLeastLoadedUser для команды с ID: {teamId}");

            var userIds = GetUsersByTerritory(teamId, tracingService);

            if (userIds == null || userIds.Count == 0)
            {
                tracingService.Trace("Не найдено пользователей для данной команды.");
                return null;
            }

            tracingService.Trace($"Количество пользователей для команды {teamId}: {userIds.Count}");

            EntityReference leastLoadedUser = null;
            int leastLoad = int.MaxValue;

            foreach (var userId in userIds)
            {
                if (userId == Guid.Empty)
                {
                    tracingService.Trace($"Пропуск пользователя с Guid.Empty.");
                    continue;
                }

                var userLoad = GetUserLoad(userId, tracingService);
                if (userLoad < leastLoad)
                {
                    leastLoad = userLoad;
                    leastLoadedUser = new EntityReference("systemuser", userId);
                }
            }

            return leastLoadedUser;
        }







        public List<Guid> GetUsersByTerritory(Guid territoryId, ITracingService tracingService)
        {
            tracingService.Trace($"Получение пользователей для территории с ID: {territoryId}");

            // Получаем все команды, связанные с территорией
            var query = new QueryExpression("nk_nkcountries_team") // Таблица связи
            {
                ColumnSet = new ColumnSet("teamid"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("nk_nkcountriesid", ConditionOperator.Equal, territoryId)
            }
                }
            };

            tracingService.Trace("Выполнение запроса для получения связанных команд...");
            var teamResults = _service.RetrieveMultiple(query);

            if (teamResults.Entities.Count == 0)
            {
                tracingService.Trace("Для данной территории нет связанных команд.");
                return new List<Guid>();
            }

            // Для каждой команды получить пользователей
            List<Guid> userIds = new List<Guid>();
            foreach (var team in teamResults.Entities)
            {
                Guid teamId = team.GetAttributeValue<Guid>("teamid");
                tracingService.Trace($"Получение пользователей для команды {teamId}");

                var userQuery = new QueryExpression("teammembership")
                {
                    ColumnSet = new ColumnSet("systemuserid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                {
                    new ConditionExpression("teamid", ConditionOperator.Equal, teamId)
                }
                    }
                };

                var userResults = _service.RetrieveMultiple(userQuery);

                foreach (var user in userResults.Entities)
                {
                    Guid userId = user.GetAttributeValue<Guid>("systemuserid");
                    if (!userIds.Contains(userId))
                    {
                        userIds.Add(userId);
                    }
                }
            }

            tracingService.Trace($"Найдено {userIds.Count} пользователей для территории.");
            return userIds;
        }


        public List<Guid> GetTeamsByTerritory(Guid territoryId, ITracingService tracingService)
        {
            tracingService.Trace($"Получение рабочих групп для территории с ID: {territoryId}");

            var query = new QueryExpression("nk_nkcountries")
            {
                ColumnSet = new ColumnSet(false),
                Criteria =
        {
            Conditions =
            {
                new ConditionExpression("nk_nkcountryid", ConditionOperator.Equal, territoryId)
            }
        },
                LinkEntities =
        {
            new LinkEntity
            {
                LinkFromEntityName = "nk_nkcountries",
                LinkFromAttributeName = "nk_nkcountryid",
                LinkToEntityName = "teammembership",
                LinkToAttributeName = "teamid",
                Columns = new ColumnSet("teamid"),
                EntityAlias = "team"
            }
        }
            };

            var results = _service.RetrieveMultiple(query);
            var teamIds = results.Entities.Select(e => e.GetAttributeValue<Guid>("team.teamid")).ToList();

            tracingService.Trace($"Найдено {teamIds.Count} рабочих групп для территории.");
            return teamIds;
        }


        public void UpdateEntity(Entity entity)
        {
            _service.Update(entity);
        }
    }
}
