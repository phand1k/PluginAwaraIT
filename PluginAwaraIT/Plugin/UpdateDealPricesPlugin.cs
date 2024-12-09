using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;

namespace PluginAwaraIT.Plugin
{
    public class UpdateDealPricesPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Получение сервисов из контекста
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Проверка, что плагин вызван на создании записи "Продуктовая корзина"
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity courseBasket)
            {
                try
                {
                    // Получение ID связанной "Возможной сделки"
                    if (courseBasket.Contains("nk_dealid") && courseBasket["nk_dealid"] is EntityReference dealRef)
                    {
                        Guid dealId = dealRef.Id;

                        // Запрос всех связанных "Продуктовых корзин"
                        var query = new QueryExpression("nk_basket")
                        {
                            ColumnSet = new ColumnSet("nk_beforebaseprice", "nk_discount", "nk_afterbaseprice"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("nk_dealid", ConditionOperator.Equal, dealId)
                                }
                            }
                        };

                        var productBaskets = service.RetrieveMultiple(query);

                        // Инициализация сумм
                        decimal totalBasePrice = 0m;
                        decimal totalDiscountAmount = 0m; // Общая сумма абсолютной скидки
                        decimal totalPriceAfterDiscount = 0m;

                        // Суммирование значений
                        foreach (var basket in productBaskets.Entities)
                        {
                            // Проверка и получение значения nk_beforebaseprice
                            if (basket.Contains("nk_beforebaseprice"))
                            {
                                var basePriceValue = basket["nk_beforebaseprice"];
                                if (basePriceValue is Money moneyValue)
                                {
                                    totalBasePrice += moneyValue.Value;
                                }
                                else if (basePriceValue is int intValue)
                                {
                                    totalBasePrice += intValue;
                                }
                            }

                            // Проверка и получение значения nk_discount
                            if (basket.Contains("nk_discount"))
                            {
                                var discountValue = basket["nk_discount"];
                                if (discountValue is Money moneyValue)
                                {
                                    totalDiscountAmount += moneyValue.Value;
                                }
                                else if (discountValue is int intValue)
                                {
                                    // Если скидка указана как процент (≤ 100), рассчитать её как процент от базовой цены
                                    if (intValue <= 100)
                                    {
                                        totalDiscountAmount += totalBasePrice * (intValue / 100m);
                                    }
                                    else
                                    {
                                        // Если скидка указана как абсолютное значение
                                        totalDiscountAmount += intValue;
                                    }
                                }
                            }

                            // Проверка и получение значения nk_afterbaseprice
                            if (basket.Contains("nk_afterbaseprice"))
                            {
                                var afterBasePriceValue = basket["nk_afterbaseprice"];
                                if (afterBasePriceValue is Money moneyValue)
                                {
                                    totalPriceAfterDiscount += moneyValue.Value;
                                }
                                else if (afterBasePriceValue is int intValue)
                                {
                                    totalPriceAfterDiscount += intValue;
                                }
                            }
                        }

                        // Обновление полей в "Возможной сделке"
                        var dealToUpdate = new Entity("nk_nkpossibledeal", dealId)
                        {
                            ["nk_baseprice"] = new Money(totalBasePrice),
                            ["nk_discountsumm"] = new Money(totalDiscountAmount),
                            ["nk_beforediscountpricesumm"] = new Money(totalBasePrice - totalDiscountAmount)
                        };

                        service.Update(dealToUpdate);
                    }
                }
                catch (Exception ex)
                {
                    tracingService.Trace($"Ошибка в плагине UpdateDealPricesPlugin: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
