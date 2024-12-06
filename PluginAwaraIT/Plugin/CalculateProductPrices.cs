using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace PluginAwaraIT.Workflow
{
    public class CalculateProductPrices : CodeActivity
    {
        [Input("DealId")]
        [ReferenceTarget("nk_nkpossibledeal")] // Логическое имя таблицы "Возможная сделка"

        public InArgument<EntityReference> DealId { get; set; }

        [Input("CourseId")]
        [ReferenceTarget("nk_nkcourses")] // Логическое имя таблицы "Курсы"
        public InArgument<EntityReference> CourseId { get; set; }



        [Input("Discount")]
        public InArgument<int> Discount { get; set; }

        [Output("BasePrice")]
        public OutArgument<Money> BasePrice { get; set; }

        [Output("priceAfterDiscount")]
        public OutArgument<Money> PriceAfterDiscount { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            var tracingService = context.GetExtension<ITracingService>();

            tracingService.Trace("Начало выполнения CalculateProductPrices");

            try
            {
                // Получение входных параметров
                var dealId = DealId.Get(context);
                var courseId = CourseId.Get(context);

                if (dealId == null || courseId == null)
                {
                    throw new InvalidWorkflowException("DealId и CourseId обязательны для выполнения.");
                }

                var discount = Discount.Get(context);

                tracingService.Trace($"DealId: {dealId.Id}, CourseId: {courseId.Id}, Discount: {discount}");

                // Получение данных сделки
                var deal = service.Retrieve("nk_nkpossibledeal", dealId.Id, new ColumnSet("nk_territoryid"));
                var territoryId = deal.GetAttributeValue<EntityReference>("nk_territoryid")?.Id;
                tracingService.Trace($"Территория сделки: {territoryId}");

                // Поиск активного прайс-листа
                var currentDate = DateTime.UtcNow;
                var priceListQuery = new QueryExpression("nk_nkpricelistcourses")
                {
                    ColumnSet = new ColumnSet("nk_nkpricelistcoursesid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                {
                    new ConditionExpression("nk_startdate", ConditionOperator.LessEqual, currentDate),
                    new ConditionExpression("nk_enddate", ConditionOperator.GreaterEqual, currentDate)
                }
                    }
                };

                var priceLists = service.RetrieveMultiple(priceListQuery);
                if (!priceLists.Entities.Any())
                {
                    throw new InvalidWorkflowException("Активный прайс-лист не найден.");
                }

                var activePriceListId = priceLists.Entities[0].Id;
                tracingService.Trace($"Активный прайс-лист: {activePriceListId}");

                // Поиск позиции прайс-листа
                var query = new QueryExpression("nk_pricelistposition")
                {
                    ColumnSet = new ColumnSet("nk_price", "nk_preparationformatid", "nk_conductformatid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                {
                    new ConditionExpression("nk_territoryid", ConditionOperator.Equal, territoryId),
                    new ConditionExpression("nk_courseid", ConditionOperator.Equal, courseId.Id),
                    new ConditionExpression("nk_pricelistid", ConditionOperator.Equal, activePriceListId)
                }
                    }
                };

                var priceListItems = service.RetrieveMultiple(query);
                tracingService.Trace($"Найдено записей в прайс-листе: {priceListItems.Entities.Count}");

                if (!priceListItems.Entities.Any())
                {
                    throw new InvalidWorkflowException("Позиция прайс-листа не найдена для указанных параметров.");
                }

                var priceListItem = priceListItems.Entities.First();
                var priceMoney = priceListItem.GetAttributeValue<Money>("nk_price");
                if (priceMoney == null)
                {
                    throw new InvalidWorkflowException("Цена в позиции прайс-листа отсутствует.");
                }
                var price = priceMoney.Value;
                var preparationFormatId = priceListItem.GetAttributeValue<EntityReference>("nk_preparationformatid")?.Id;
                var conductFormatId = priceListItem.GetAttributeValue<EntityReference>("nk_conductformatid")?.Id;

                tracingService.Trace($"Цена из прайс-листа: {price}");
                tracingService.Trace($"Форматы: preparationFormatId={preparationFormatId}, conductFormatId={conductFormatId}");

                // Рассчет цены после скидки
                var priceAfterDiscount = price * (1 - ((decimal)discount / 100));
                tracingService.Trace($"Цена после скидки (расчет): {priceAfterDiscount}");
                tracingService.Trace($"BasePrice отправлен: {price}");
                tracingService.Trace($"PriceAfterDiscount отправлен: {priceAfterDiscount}");
                BasePrice.Set(context, new Money(price));
                PriceAfterDiscount.Set(context, new Money(priceAfterDiscount));

                // Установка выходных параметров
                BasePrice.Set(context, new Money(price));
                PriceAfterDiscount.Set(context, new Money(priceAfterDiscount));
                tracingService.Trace($"BasePrice отправлен: {price}");
                tracingService.Trace($"PriceAfterDiscount отправлен: {priceAfterDiscount}");

                tracingService.Trace("Выполнение CalculateProductPrices завершено успешно.");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Ошибка: {ex.Message}");
                throw new InvalidWorkflowException($"Ошибка в действии: {ex.Message}", ex);
            }
        }

    }
}
