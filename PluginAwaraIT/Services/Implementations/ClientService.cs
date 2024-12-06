using Microsoft.Xrm.Sdk;
using PluginAwaraIT.Mapping;
using PluginAwaraIT.Repositories.Interfaces;
using System;
using System.Linq;

namespace PluginAwaraIT.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly IRepository _repository;
        private readonly ITracingService _tracingService;

        public ClientService(IRepository repository, ITracingService tracingService)
        {
            _repository = repository;
            _tracingService = tracingService;
        }

        public void ProcessInterest(Entity targetEntity)
        {
            _tracingService.Trace("Начало обработки интереса...");

            var email = targetEntity.GetAttributeValue<string>(ColumnMap.Interest.Email);
            var phone = targetEntity.GetAttributeValue<string>(ColumnMap.Interest.Phone);
            var firstName = targetEntity.GetAttributeValue<string>(ColumnMap.Interest.FirstName);
            var lastName = targetEntity.GetAttributeValue<string>(ColumnMap.Interest.LastName);
            if (targetEntity.Attributes.Contains(ColumnMap.Interest.CountryId))
            {
                var value = targetEntity[ColumnMap.Interest.CountryId];
                _tracingService.Trace($"Тип атрибута {ColumnMap.Interest.CountryId}: {value?.GetType().FullName}");
            }
            var countryReference = targetEntity.GetAttributeValue<EntityReference>(ColumnMap.Interest.CountryId);
            _tracingService.Trace($"Email: {email}, Phone: {phone}");

            // Поиск или создание карточки клиента
            var clientCard = _repository.FindClientCard(email, phone);
            if (clientCard == null)
            {
                _tracingService.Trace("Карточка клиента не найдена. Создание новой карточки клиента...");

                clientCard = new Entity(ColumnMap.ClientCard.EntityLogicalName)
                {
                    [ColumnMap.ClientCard.FirstName] = firstName,
                    [ColumnMap.ClientCard.LastName] = lastName,
                    [ColumnMap.ClientCard.Email] = email,
                    [ColumnMap.ClientCard.Phone] = phone,
                    [ColumnMap.ClientCard.Name] = $"{firstName} {lastName}",
                    [ColumnMap.ClientCard.CountryId] = countryReference
                };

                var clientCardId = _repository.CreateClientCard(clientCard);
                clientCard.Id = clientCardId;

                _tracingService.Trace($"Создана карточка клиента с ID: {clientCardId}");
            }

            var updateEntity = new Entity(ColumnMap.Interest.EntityLogicalName, targetEntity.Id)
            {
                [ColumnMap.Interest.ClientId] = new EntityReference(ColumnMap.ClientCard.EntityLogicalName, clientCard.Id)
            };
            /// <summary>
            /// Статическое назначение id группы коллцентра
            /// </summary>

            // Назначение наименее загруженного пользователя из группы колл-центра
            var callCenterTeamId = new Guid("f04ce894-faaf-ef11-b8e9-000d3a5c09a6"); // ID группы колл-центра
            _tracingService.Trace($"Идентификатор команды колл-центра: {callCenterTeamId}");

            var leastLoadedUser = _repository.FindLeastLoadedUserForCallCenter(callCenterTeamId, _tracingService);
            if (leastLoadedUser != null)
            {
                updateEntity[ColumnMap.Interest.OwnerId] = leastLoadedUser;
                _tracingService.Trace($"Назначен наименее загруженный пользователь: {leastLoadedUser.Id}");
            }

            _repository.UpdateEntity(updateEntity);
            _tracingService.Trace("Интерес успешно обновлён.");
        }

        public void ProcessInterestStatusUpdate(Entity postImage)
        {
            _tracingService.Trace("Начало обработки обновления статуса интереса...");

            var statusCode = postImage.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? -1;
            var territoryId = postImage.GetAttributeValue<EntityReference>("nk_nkterritory")?.Id;

            if (territoryId == Guid.Empty)
            {
                _tracingService.Trace("Территория не указана. Невозможно назначить ответственного.");
                return;
            }

            if (statusCode != 2) // 2 - "Согласие"
            {
                _tracingService.Trace("Статус интереса не равен 'Согласие'. Обработка завершена.");
                return;
            }

            _tracingService.Trace($"Обработка назначения ответственного для территории с ID: {territoryId}...");

            var teamIds = _repository.GetTeamsByTerritory(territoryId.Value, _tracingService);

            if (teamIds == null || teamIds.Count == 0)
            {
                _tracingService.Trace("Для территории не найдено рабочих групп.");
                return;
            }

            EntityReference leastLoadedUser = null;
            int leastLoad = int.MaxValue;

            foreach (var teamId in teamIds)
            {
                try
                {
                    var user = _repository.FindLeastLoadedUser(teamId, _tracingService);
                    var userLoad = _repository.GetUserLoad(user.Id, _tracingService);

                    if (userLoad < leastLoad)
                    {
                        leastLoad = userLoad;
                        leastLoadedUser = user;
                    }
                }
                catch (Exception ex)
                {
                    _tracingService.Trace($"Ошибка при обработке рабочей группы {teamId}: {ex.Message}");
                }
            }

            if (leastLoadedUser == null)
            {
                _tracingService.Trace("Не удалось найти наименее загруженного пользователя.");
                return;
            }

            _tracingService.Trace($"Назначен ответственный: {leastLoadedUser.Id}");

            postImage["ownerid"] = leastLoadedUser;
            _repository.UpdateEntity(postImage);
        }

    }
}
