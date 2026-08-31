using System;
using System.Collections.Generic;
using System.Linq;

namespace NuclearOptionDetents.Core;

internal readonly struct DetectedAircraftIdentity
{
    public DetectedAircraftIdentity(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }
    public string DisplayName { get; }
}

/// <summary>Persistent, deterministic list of aircraft IDs found in the installed catalog or during seat entry.</summary>
internal sealed class DetectedAircraftCatalog
{
    private readonly Dictionary<string, string> _displayNames =
        new(StringComparer.OrdinalIgnoreCase);

    public DetectedAircraftCatalog(string serialized)
    {
        foreach (var record in (serialized ?? string.Empty).Split(';'))
        {
            var separator = record.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var escapedId = record.Substring(0, separator);
            var escapedDisplayName = record.Substring(separator + 1);
            if (!HasValidPercentEncoding(escapedId) || !HasValidPercentEncoding(escapedDisplayName))
            {
                continue;
            }

            try
            {
                var id = Uri.UnescapeDataString(escapedId).Trim();
                var displayName = Uri.UnescapeDataString(escapedDisplayName).Trim();
                Register(id, displayName);
            }
            catch (UriFormatException)
            {
                // Ignore malformed hand-edited records; a bad catalog must not stop the mod loading.
            }
        }
    }

    public IReadOnlyList<DetectedAircraftIdentity> All =>
        _displayNames
            .Select(pair => new DetectedAircraftIdentity(pair.Key, pair.Value))
            .OrderBy(identity => identity.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(identity => identity.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool Contains(string id) =>
        !string.IsNullOrWhiteSpace(id) && _displayNames.ContainsKey(id.Trim());

    public bool Register(string id, string displayName)
    {
        id = (id ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return false;
        }

        displayName = (displayName ?? string.Empty).Trim();
        if (displayName.Length == 0)
        {
            displayName = id;
        }

        if (_displayNames.TryGetValue(id, out var existing) &&
            string.Equals(existing, displayName, StringComparison.Ordinal))
        {
            return false;
        }

        _displayNames[id] = displayName;
        return true;
    }

    public string DisplayNameFor(string id) =>
        _displayNames.TryGetValue(id ?? string.Empty, out var displayName)
            ? displayName
            : id ?? string.Empty;

    public string Serialize() =>
        string.Join(
            ";",
            _displayNames
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair =>
                    $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    private static bool HasValidPercentEncoding(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length || !IsHex(value[index + 1]) || !IsHex(value[index + 2]))
            {
                return false;
            }

            index += 2;
        }

        return true;
    }

    private static bool IsHex(char value) =>
        value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
