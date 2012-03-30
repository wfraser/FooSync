///
/// Codewise/FooSync/Daemon/Installer.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using NetFwTypeLib;

namespace Codewise.FooSync.Daemon
{
    /// <summary>
    /// Windows service installer for FooSync Daemon
    /// 
    /// Install the service by running from an elevated cmd prompt:
    ///     %windir%\Microsoft.NET\Framework\[version]\InstallUtil.exe FooSync.Daemon.exe
    /// </summary>
    [RunInstaller(true)]
    public class FooSyncServiceInstaller : Installer
    {
        public FooSyncServiceInstaller()
        {
            var process = new ServiceProcessInstaller();

            process.Account = ServiceAccount.LocalSystem;

            var serviceAdmin = new ServiceInstaller();

            serviceAdmin.StartType = ServiceStartMode.Manual;
            serviceAdmin.ServiceName = "FooSyncService";
            serviceAdmin.DisplayName = "FooSync Daemon";
            serviceAdmin.Description = "Serves FooSync repositories across the network.";

            Installers.Add(process);
            Installers.Add(serviceAdmin);
        }

#if !__MonoCS__
        /// <summary>
        /// This region is specific to WinNT, namely the Windows Firewall with Advanced Security
        /// This code probably will also not work prior to Windows Vista, but this is untested.
        /// </summary>

        static readonly string FirewallRuleName = "FooSync Daemon";

        protected override void OnAfterInstall(System.Collections.IDictionary savedState)
        {
            AddFirewallRule();
            base.OnAfterInstall(savedState);
        }

        public void AddFirewallRule()
        {
            System.Diagnostics.Debugger.Launch();

            Type NetFwPolicy2Type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (NetFwPolicy2Type == null)
            {
                Console.WriteLine("Couldn't get instance of NetFwPolicy2");
                return;
            }

            var policy = (INetFwPolicy2)Activator.CreateInstance(NetFwPolicy2Type);

            foreach (INetFwRule r in policy.Rules)
            {
                if (r != null && r.Name != null && r.Name.Equals(FirewallRuleName))
                {
                    Console.WriteLine("Not adding FW rule; it already exists.");
                    return;
                }
            }

            Type NetFwRuleType = Type.GetTypeFromProgID("HNetCfg.FWRule", false);
            if (NetFwRuleType == null)
            {
                Console.WriteLine("Couldn't get instance of INetFwRule");
                return;
            }

            var rule = (INetFwRule)Activator.CreateInstance(NetFwRuleType);
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            rule.Description = "Allow FooSync Daemon to accept remote connections";
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.Enabled = true;
            rule.InterfaceTypes = "All";
            rule.Name = FirewallRuleName;
            rule.ApplicationName = System.Reflection.Assembly.GetExecutingAssembly().Location;

            policy.Rules.Add(rule);
        }

        protected override void OnAfterUninstall(System.Collections.IDictionary savedState)
        {
            RemoveFirewallRule();
            base.OnAfterUninstall(savedState);
        }

        private void RemoveFirewallRule()
        {
            System.Diagnostics.Debugger.Launch();

            Type NetFwPolicy2Type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (NetFwPolicy2Type == null)
            {
                Console.WriteLine("Couldn't get instance of NetFwPolicy2");
                return;
            }

            var policy = (INetFwPolicy2)Activator.CreateInstance(NetFwPolicy2Type);
            policy.Rules.Remove(FirewallRuleName);
        }
#endif // !__MonoCS__
    }
}
