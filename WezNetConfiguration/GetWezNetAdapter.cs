using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WezNetConfiguration.Models;
using WezNetConfiguration.NativeCode.NetConfig;

namespace WezNetConfiguration
{
    [Cmdlet("Get", "WezNetAdapter")]
    public class GetWezNetAdapter : PSCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string DisplayName { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string BindName { get; set; }

        protected override void ProcessRecord()
        {
            var adapters = new List<NetAdapter>();

            INetCfg netConfig = null;
            INetCfgClass netConfigClass = null;
            INetCfgLock netLock = null;

            try
            {
                netConfig = NetConfigExtensions.GetNetConfigInstance();
                netLock = netConfig.ToInitializedLock();

                var netCfgClassObj = new object();
                netConfig.QueryNetCfgClass(ref NetCfgGuids.IidDevClassNet, ref NetCfgGuids.IidINetCfgClass, out netCfgClassObj);

                netConfigClass = (netCfgClassObj as INetCfgClass);

                var components = netConfigClass.GetComponents();
                var component = components.GetNextComponent();

                do
                {
                    var displayName = string.Empty;
                    var bindName = string.Empty;

                    component.GetBindName(out bindName);
                    component.GetDisplayName(out displayName);

                    if (!String.IsNullOrEmpty(DisplayName) && displayName.Replace("*", "").StartsWith(DisplayName))
                    {
                        var adapter = new NetAdapter(displayName, bindName);
                        adapters.Add(adapter);
                    }
                    else if (!String.IsNullOrEmpty(BindName) && bindName.Replace("*", "").StartsWith(BindName))
                    {
                        var adapter = new NetAdapter(displayName, bindName);
                        adapters.Add(adapter);
                    }
                    else if (String.IsNullOrEmpty(BindName) && String.IsNullOrEmpty(DisplayName))
                    {
                        var adapter = new NetAdapter(displayName, bindName);
                        adapters.Add(adapter);
                    }

                    component = components.GetNextComponent();
                } while (component != null);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (netLock != null)
                    netLock.ReleaseWriteLock();

                netConfig.Uninitialize();
                netConfig = null;

                GC.Collect();
            }

            WriteObject(adapters);
        }
    }
}
