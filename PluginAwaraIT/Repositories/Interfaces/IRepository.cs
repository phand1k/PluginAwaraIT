using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Repositories.Interfaces
{
    public interface IRepository
    {
        int GetUserLoadForCallCenter(Guid userId, ITracingService tracingService);
        int GetUserLoad(Guid userId, ITracingService tracingService);
        Entity FindClientCard(string email, string phone);
        Guid CreateClientCard(Entity clientCard);
        EntityReference FindLeastLoadedUser(Guid teamId, ITracingService tracingService);
        EntityReference FindLeastLoadedUserForCallCenter(Guid teamId, ITracingService tracingService);
        void UpdateEntity(Entity entity);
        List<Guid> GetTeamsByTerritory(Guid territoryId, ITracingService tracingService);
    }
}
