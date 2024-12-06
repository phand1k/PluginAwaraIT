using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Services
{
    public interface IClientService
    {
        void ProcessInterest(Entity targetEntity);
    }

}
