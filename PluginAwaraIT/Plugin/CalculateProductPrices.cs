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
        [Input("dealid")]
        [ReferenceTarget("nk_nkpossibledeal")] // Логическое имя таблицы "Возможная сделка"

        public InArgument<EntityReference> DealId { get; set; }

        [Input("nk_courseid")]
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
                var discount = Discount.Get(context);

                if (dealId == null || courseId == null)
                {
                    throw new InvalidWorkflowException("DealId и CourseId обязательны для выполнения.");
                }

                tracingService.Trace($"DealId: {dealId.Id}, CourseId: {courseId.Id}, Discount: {discount}");

                // Шаг 1: Получение данных курса
                tracingService.Trace("Получение данных курса...");
                var course = service.Retrieve("nk_nkcourses", courseId.Id, new ColumnSet("nk_preparationformatcourseid", "nk_formatcourseid", "nk_territoryid"));
                var preparationFormatId = course.GetAttributeValue<EntityReference>("nk_preparationformatcourseid")?.Id;
                var formatCourseId = course.GetAttributeValue<EntityReference>("nk_formatcourseid")?.Id;
                var territoryId = course.GetAttributeValue<EntityReference>("nk_territoryid")?.Id;

                tracingService.Trace($"Данные курса: PreparationFormatId: {preparationFormatId}, FormatCourseId: {formatCourseId}, TerritoryId: {territoryId}");

                if (preparationFormatId == null || formatCourseId == null || territoryId == null)
                {
                    throw new InvalidWorkflowException("Некоторые данные курса отсутствуют (формат подготовки, формат проведения или территория).");
                }

                // Шаг 2: Поиск позиции прайс-листа
                tracingService.Trace("Поиск позиции прайс-листа...");
                var query = new QueryExpression("nk_pricelistposition")
                {
                    ColumnSet = new ColumnSet("nk_price"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                {
                    new ConditionExpression("nk_preparationformatid", ConditionOperator.Equal, preparationFormatId),
                    new ConditionExpression("nk_conductformatid", ConditionOperator.Equal, formatCourseId),
                    new ConditionExpression("nk_territoryid", ConditionOperator.Equal, territoryId)
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

                tracingService.Trace($"Цена из прайс-листа: {price}");

                // Шаг 3: Рассчет цены после скидки
                var priceAfterDiscount = price;
                if (discount > 100)
                {
                    priceAfterDiscount = price - discount;
                }
                else
                {
                    priceAfterDiscount = price * (1 - ((decimal)discount / 100));
                }
                
                tracingService.Trace($"Цена после скидки (расчет): {priceAfterDiscount}");
                BasePrice.Set(context, new Money(price));
                PriceAfterDiscount.Set(context, new Money(priceAfterDiscount));
                tracingService.Trace($"Проверка значения PriceAfterDiscount перед отправкой: {priceAfterDiscount}");


                // Установка выходных параметров
                BasePrice.Set(context, new Money(price));
                PriceAfterDiscount.Set(context, new Money(priceAfterDiscount));
                tracingService.Trace($"BasePrice отправлен: {price}");
                tracingService.Trace($"PriceAfterDiscount отправлен: {priceAfterDiscount}");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Ошибка: {ex.Message}");
                throw new InvalidWorkflowException($"Ошибка в действии: {ex.Message}", ex);
            }
        }



    }
}
