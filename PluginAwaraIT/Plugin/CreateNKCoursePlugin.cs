using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PluginAwaraIT.Repositories.Implementations;
using PluginAwaraIT.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Plugin
{
    /// <summary>
    /// Формирование name для курса (карточка продукта)
    /// </summary>
    public class CreateNKCoursePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                var target = (Entity)context.InputParameters["Target"];

                if (target.LogicalName != "nk_nkcourses") return;

                if (target.Contains("nk_subjectit"))
                {
                    var subjectReference = (EntityReference)target["nk_subjectit"];
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
                    if (target.Contains("nk_preparationformatcourseid"))
                    {
                        var formatReference = (EntityReference)target["nk_preparationformatcourseid"];
                        var format = service.Retrieve("nk_nkpreparationformatcourse", formatReference.Id, new ColumnSet("nk_name"));
                        formatName = format.Contains("nk_name") ? format["nk_name"].ToString() : string.Empty;
                    }
                    var formatCourseName = string.Empty;
                    if (target.Contains("nk_formatcourseid"))
                    {
                        var formatCourseReference = (EntityReference)target["nk_formatcourseid"];
                        var format = service.Retrieve("nk_nkformatcourse", formatCourseReference.Id, new ColumnSet("nk_name"));
                        formatCourseName = format.Contains("nk_name") ? format["nk_name"].ToString() : string.Empty;
                    }

                    string courseName = $"{subjectName}, {territoryName}, {formatName}, {formatCourseName}";
                    target["nk_name"] = courseName;

                    service.Update(target);
                }
                else
                {
                    tracingService.Trace("error");
                }
            }
        }
    }
}
