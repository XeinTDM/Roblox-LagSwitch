using NetFwTypeLib;

namespace RobloxLagswitch;

public static class NetworkManager
{
    public static void EnsureRuleExists(string processPath, string ruleName)
    {
        try
        {
            var firewallPolicy = Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2")) as INetFwPolicy2;

            var firewallRule = firewallPolicy.Rules.OfType<INetFwRule>()
                .FirstOrDefault(r => r.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

            if (firewallRule == null)
            {
                firewallRule = (INetFwRule)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FWRule"))!;

                firewallRule.Name = ruleName;
                firewallRule.Description = $"Block {System.IO.Path.GetFileName(processPath)}";
                firewallRule.ApplicationName = processPath;
                firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                firewallRule.Enabled = false;
                firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
                firewallRule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;

                firewallPolicy.Rules.Add(firewallRule);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Firewall rule error: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public static bool SetProcessBlockState(string processPath, string ruleName, bool enable)
    {
        try
        {
            var firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            var firewallRule = firewallPolicy.Rules.OfType<INetFwRule>()
                .FirstOrDefault(r => r.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

            if (firewallRule == null) return false;

            firewallRule.Enabled = enable;
            return true;
        } catch { return false; }
    }

    public static bool IsBlocked(string processPath, string ruleName)
    {
        try
        {
            var firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            return firewallPolicy.Rules.OfType<INetFwRule>()
                .Any(r => r.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase) &&
                         r.ApplicationName.Equals(processPath, StringComparison.OrdinalIgnoreCase) &&
                         r.Enabled);
        } catch { return false; }
    }
}