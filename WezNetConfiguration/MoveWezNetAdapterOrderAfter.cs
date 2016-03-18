﻿using System;
using System.Management.Automation;
using System.Runtime.InteropServices;
using WezNetConfiguration.NativeCode.NetConfig;

namespace WezNetConfiguration
{
    [Cmdlet("Move", "WezNetAdapterOrderAfter")]
    public class MoveWezNetAdapterOrderAfter : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Adapter { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string PlaceAfter { get; set; }

        protected override void ProcessRecord()
        {
            INetCfg netConfig = null;
            INetCfgClass netConfigClass = null;
            INetCfgLock netLock = null;

            try
            {
                netConfig = NetConfigExtensions.GetNetConfigInstance();
                netLock = netConfig.ToInitializedLock();

                //netConfigClass = (netConfig as INetCfgClass);
                var netCfgClassObj = new object();
                netConfig.QueryNetCfgClass(ref NetCfgGuids.IidDevClassNet, ref NetCfgGuids.IidINetCfgClass, out netCfgClassObj);

                // Find path tokens for each device we want to move
                var adapterToMoveToken = string.Empty;
                var adapterToReplaceToken = string.Empty;
                var adapterToMove = default(INetCfgComponent);
                var adapterToReplace = default(INetCfgComponent);

                netConfigClass = (netCfgClassObj as INetCfgClass);

                var components = netConfigClass.GetComponents();
                var component = components.GetNextComponent();

                do
                {
                    var displayName = string.Empty;
                    var bindName = string.Empty;
                    var nodeId = string.Empty;

                    component.GetPnpDevNodeId(out nodeId);
                    component.GetBindName(out bindName);
                    component.GetDisplayName(out displayName);

                    if (bindName.Equals(Adapter))
                    {
                        adapterToMoveToken = nodeId;
                        adapterToMove = component;
                    }
                    else if (bindName.Equals(PlaceAfter))
                    {
                        adapterToReplaceToken = nodeId;
                        adapterToReplace = component;
                    }

                    component = components.GetNextComponent();
                } while (component != null);

                // Enumerate the binding paths and rework the list
                object rawComponent;
                var baseComponent = netConfig.FindComponent("ms_tcpip", out rawComponent);
                var pNetCfgBinding = (rawComponent as INetCfgComponentBindings);

                object pEnumNetCfgBindingPath;
                pNetCfgBinding.EnumBindingPaths((int)BindingPathsFlags.Below, out pEnumNetCfgBindingPath);
                var bindingPathEnum = (pEnumNetCfgBindingPath as IEnumNetCfgBindingPath);
                bindingPathEnum.Reset();

                var bindingToMove = default(INetCfgBindingPath);
                var bindingToReplace = default(INetCfgBindingPath);

                do
                {
                    var binding = bindingPathEnum.GetNextBindingPath();
                    if (binding == null)
                        break;

                    var pathToken = string.Empty;
                    binding.GetPathToken(out pathToken);

                    if (pathToken.EndsWith(adapterToMoveToken))
                        bindingToMove = binding;
                    else if (pathToken.EndsWith(adapterToReplaceToken))
                        bindingToReplace = binding;
                }
                while (true);

                if (bindingToMove != null && bindingToReplace != null)
                {
                    pNetCfgBinding.MoveAfter(bindingToMove, bindingToReplace);
                    netConfig.Apply();
                }
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
        }
    }
}
