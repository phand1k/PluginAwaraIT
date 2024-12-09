function onProductOrDiscountChange(executionContext) {
    const formContext = executionContext.getFormContext();

    const dealReference = formContext.getAttribute("nk_dealid")?.getValue();
    const courseReference = formContext.getAttribute("nk_courseid")?.getValue();
    let discount = formContext.getAttribute("nk_discount")?.getValue();

    if (!dealReference || !courseReference) {
        console.error("Сделка или курс не выбраны.");
        return;
    }

    const dealId = dealReference[0]?.id.replace(/[{}]/g, "");
    const courseId = courseReference[0]?.id.replace(/[{}]/g, "");

    if (!discount || isNaN(discount)) {
        discount = 0;
    }
    const basePriceField = formContext.getAttribute("nk_beforebaseprice");
    const afterDiscountField = formContext.getAttribute("nk_afterbaseprice");
    
    
    console.log("Перед вызовом Action:");
    console.log("DealId:", dealId);
    console.log("CourseId:", courseId);
    console.log("Discount:", discount);

    const actionRequest = {
        DealId: { entityType: "nk_nkpossibledeal", id: dealId },
        CourseId: { entityType: "nk_nkcourses", id: courseId },
        Discount: parseInt(discount, 10),
        getMetadata: function () {
            return {
                operationName: "nk_CalculateProductPrices1",
                parameterTypes: {
                    DealId: { typeName: "Microsoft.Dynamics.CRM.EntityReference", structuralProperty: 5 },
                    CourseId: { typeName: "Microsoft.Dynamics.CRM.EntityReference", structuralProperty: 5 },
                    Discount: { typeName: "Edm.Int32", structuralProperty: 1 }
                },
                operationType: 0
            };
        }
    };

    console.log("Параметры для Action:", JSON.stringify(actionRequest, null, 2));

    Xrm.WebApi.online.execute(actionRequest).then(
        function success(response) {
            if (response.ok) {
                response.json().then(function (outputParameters) {
                    console.log("Response Output Parameters:", outputParameters);
    
                    const basePrice = outputParameters.basePrice ?? 0;
                    const priceAfterDiscount = outputParameters.priceAfterDiscount ?? 0;
                    if (basePriceField) {
                        console.error("Поле 'nk_beforebaseprice' найдено.");
                    } else {
                        console.error("Поле 'nk_beforebaseprice' не найдено.");
                    }
                    
                    if (afterDiscountField) {
                        console.error("Поле 'nk_afterbaseprice' найдено.");
                    } else {
                        console.error("Поле 'nk_afterbaseprice' не найдено.");
                    }
                    formContext.getAttribute("nk_beforebaseprice").setValue(basePrice);
                    formContext.getAttribute("nk_afterbaseprice").setValue(priceAfterDiscount); 
                    
                });
            }
        },
        function error(err) {
            console.error("Ошибка при выполнении Action:", err.message);
        }
    );
    
}
