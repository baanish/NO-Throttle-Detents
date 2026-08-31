using System;
using BepInEx.Configuration;

namespace NuclearOptionDetents.Config;

// BepInEx Configuration Manager reads this optional metadata by field name.
// Keeping the soft contract local avoids a runtime dependency on that plugin.
internal sealed class ConfigurationManagerAttributes
{
    public bool? Browsable;
    public Action<ConfigEntryBase>? CustomDrawer;
    public string? DispName;
    public bool? HideDefaultButton;
    public int? Order;
    public bool? ReadOnly;
}
