using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Plugin
{
    public class CreateNKPriceListPosition : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    try
                    {
                        var target = (Entity)context.InputParameters["Target"];
                        if (target.LogicalName != "nk_pricelistposition") return;

                        if (target.Contains("nk_subjectid"))
                        {
                            var subjectReference = (EntityReference)target["nk_subjectid"];
                            var subject = service.Retrieve("nk_subject", subjectReference.Id, new ColumnSet("nk_name"));
                            string subjectName = subject.Contains("nk_name") ? subject["nk_name"].ToString() : string.Empty;
                            var territoryName = string.Empty;

                            if (target.Contains("nk_territoryid"))
                            {
                                var territoryReference = (EntityReference)target["nk_territoryid"];
                                var territory = service.Retrieve("nk_nkcountries", territoryReference.Id, new ColumnSet("nk_name"));
                                territoryName = territory.Contains("nk_name") ? territory["nk_name"].ToString() : string.Empty;
                            }
                            var formatName = string.Empty;
                            if (target.Contains("nk_preparationformatid"))
                            {
                                var formatReference = (EntityReference)target["nk_preparationformatid"];
                                var format = service.Retrieve("nk_nkpreparationformatcourse", formatReference.Id, new ColumnSet("nk_name"));
                                formatName = format.Contains("nk_name") ? format["nk_name"].ToString() : string.Empty;
                            }

                            string priceListPosition = $"{subjectName}, {territoryName}, {formatName}";
                            target["nk_name"] = priceListPosition;
                            service.Update(target);
                        }
                    }
                    catch (Exception ex)
                    {
                        tracingService.Trace($"Error: {ex.Message}");
                    }
                    
                }
                tracingService.Trace("ERror");
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("An error occurred in CoursePlugin.", ex);
            }

        }
    }
}
