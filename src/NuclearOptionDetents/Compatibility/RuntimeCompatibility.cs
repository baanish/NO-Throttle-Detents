using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Rewired;

namespace NuclearOptionDetents.Compatibility;

internal static class RuntimeCompatibility
{
    public static AccessTools.FieldRef<PilotPlayerState, Player>? PilotPlayer { get; private set; }
    public static AccessTools.FieldRef<PilotPlayerState, bool>? PilotCollective { get; private set; }
    public static AccessTools.FieldRef<PilotPlayerState, float>? PilotStrength { get; private set; }
    public static AccessTools.FieldRef<PilotPlayerState, Pilot>? PilotStatePilot { get; private set; }
    public static AccessTools.FieldRef<Pilot, Aircraft>? PilotOwnedAircraft { get; private set; }
    public static AccessTools.FieldRef<PilotPlayerState, ControlInputs>? PilotControlInputs { get; private set; }
    public static AccessTools.FieldRef<PilotPlayerState, float>? PilotSimulatedThrottle { get; private set; }
    public static Func<Aircraft, bool>? AircraftAutoHoverEnabled { get; private set; }
    public static Func<Aircraft, bool>? IsLocalAircraft { get; private set; }
    public static AccessTools.FieldRef<Airbrake, Aircraft>? AirbrakeSerializedAircraft { get; private set; }
    public static AccessTools.FieldRef<Airbrake, Aircraft>? AirbrakeAttachedAircraft { get; private set; }
    public static AccessTools.FieldRef<ControlSurface, Aircraft>? ControlSurfaceAircraft { get; private set; }
    public static AccessTools.FieldRef<ControlSurface, float>? ControlSurfaceMaxSplit { get; private set; }
    public static AccessTools.FieldRef<JetNozzle, Aircraft>? JetNozzleAircraft { get; private set; }
    public static FieldInfo? JetNozzleAfterburners { get; private set; }
    public static FieldInfo? JetNozzleAfterburnerThrottleStart { get; private set; }
    public static FieldInfo? JetNozzleAfterburnerThrottleEnd { get; private set; }
    public static FieldInfo? AircraftDefinition { get; private set; }
    public static FieldInfo? UnitDefinitionJsonKey { get; private set; }
    public static FieldInfo? UnitDefinitionName { get; private set; }

    public static void ResolveThrottleObserverFields()
    {
        ValidateDirectControlMembers(readsThrottle: true, readsNegativeThrottle: true);
        var playerField = RequireDeclaredField(typeof(PilotPlayerState), "player", typeof(Player));
        var collectiveField = RequireDeclaredField(typeof(PilotPlayerState), "collective", typeof(bool));
        var strengthField = RequireDeclaredField(typeof(PilotPlayerState), "pilotStrength", typeof(float));
        var pilotField = RequireField(typeof(PilotBaseState), "pilot", typeof(Pilot));
        var pilotAircraftField = RequireDeclaredField(typeof(Pilot), "aircraft", typeof(Aircraft));
        var inputsField = RequireField(typeof(PilotBaseState), "controlInputs", typeof(ControlInputs));
        var simulatedThrottleField = RequireDeclaredField(typeof(PilotPlayerState), "simulatedThrottle", typeof(float));
        var autoHoverMethod = AccessTools.DeclaredMethod(typeof(Aircraft), "IsAutoHoverEnabled", Type.EmptyTypes) ??
                              throw new MissingMethodException(typeof(Aircraft).FullName, "IsAutoHoverEnabled");
        if (autoHoverMethod.ReturnType != typeof(bool))
        {
            throw new InvalidOperationException("Aircraft.IsAutoHoverEnabled() does not return bool.");
        }
        var localAircraftMethod = AccessTools.DeclaredMethod(
                                      typeof(GameManager),
                                      "IsLocalAircraft",
                                      new[] { typeof(Aircraft) }) ??
                                  throw new MissingMethodException(typeof(GameManager).FullName, "IsLocalAircraft(Aircraft)");
        if (localAircraftMethod.ReturnType != typeof(bool) || !localAircraftMethod.IsStatic)
        {
            throw new InvalidOperationException("GameManager.IsLocalAircraft(Aircraft) has an unexpected shape.");
        }
        AircraftDefinition = RequireField(typeof(Aircraft), "definition", typeof(UnitDefinition));
        UnitDefinitionJsonKey = RequireDeclaredField(typeof(UnitDefinition), "jsonKey", typeof(string));
        UnitDefinitionName = RequireDeclaredField(typeof(UnitDefinition), "unitName", typeof(string));
        PilotPlayer = AccessTools.FieldRefAccess<PilotPlayerState, Player>(playerField);
        PilotCollective = AccessTools.FieldRefAccess<PilotPlayerState, bool>(collectiveField);
        PilotStrength = AccessTools.FieldRefAccess<PilotPlayerState, float>(strengthField);
        PilotStatePilot = AccessTools.FieldRefAccess<PilotPlayerState, Pilot>(pilotField);
        PilotOwnedAircraft = AccessTools.FieldRefAccess<Pilot, Aircraft>(pilotAircraftField);
        PilotControlInputs = AccessTools.FieldRefAccess<PilotPlayerState, ControlInputs>(inputsField);
        PilotSimulatedThrottle = AccessTools.FieldRefAccess<PilotPlayerState, float>(simulatedThrottleField);
        AircraftAutoHoverEnabled = AccessTools.MethodDelegate<Func<Aircraft, bool>>(autoHoverMethod);
        IsLocalAircraft = AccessTools.MethodDelegate<Func<Aircraft, bool>>(localAircraftMethod);
    }

    public static bool TryGetAirframeIdentity(Aircraft aircraft, out string id, out string displayName)
    {
        id = string.Empty;
        displayName = string.Empty;
        var definitionField = AircraftDefinition;
        var jsonKeyField = UnitDefinitionJsonKey;
        var nameField = UnitDefinitionName;
        if (definitionField is null || jsonKeyField is null || nameField is null ||
            definitionField.GetValue(aircraft) is not UnitDefinition definition)
        {
            return false;
        }

        id = jsonKeyField.GetValue(definition) as string ?? string.Empty;
        displayName = nameField.GetValue(definition) as string ?? string.Empty;
        return !string.IsNullOrEmpty(id);
    }

    public static void ResolveAirbrakeFields()
    {
        var serializedAircraftField = RequireDeclaredField(typeof(Airbrake), "aircraft", typeof(Aircraft));
        var attachedAircraftField = RequireDeclaredField(typeof(Airbrake), "attachedAircraft", typeof(Aircraft));
        AirbrakeSerializedAircraft = AccessTools.FieldRefAccess<Airbrake, Aircraft>(serializedAircraftField);
        AirbrakeAttachedAircraft = AccessTools.FieldRefAccess<Airbrake, Aircraft>(attachedAircraftField);
    }

    public static void ResolveSplitAirbrakeFields()
    {
        var aircraftField = RequireDeclaredField(typeof(ControlSurface), "aircraft", typeof(Aircraft));
        var maxSplitField = RequireDeclaredField(typeof(ControlSurface), "maxSplit", typeof(float));
        ControlSurfaceAircraft = AccessTools.FieldRefAccess<ControlSurface, Aircraft>(aircraftField);
        ControlSurfaceMaxSplit = AccessTools.FieldRefAccess<ControlSurface, float>(maxSplitField);
    }

    public static void ResolveJetNozzleFields()
    {
        var aircraftField = RequireDeclaredField(typeof(JetNozzle), "aircraft", typeof(Aircraft));
        JetNozzleAircraft = AccessTools.FieldRefAccess<JetNozzle, Aircraft>(aircraftField);
        JetNozzleAfterburners = AccessTools.DeclaredField(typeof(JetNozzle), "afterburners") ??
                                  throw new MissingFieldException(typeof(JetNozzle).FullName, "afterburners");
        var afterburnerType = JetNozzleAfterburners.FieldType.GetElementType() ??
                              throw new InvalidOperationException("JetNozzle.afterburners is not an array.");
        JetNozzleAfterburnerThrottleStart = RequireDeclaredField(afterburnerType, "throttleStart", typeof(float));
        JetNozzleAfterburnerThrottleEnd = RequireDeclaredField(afterburnerType, "throttleEnd", typeof(float));
    }

    public static bool TryGetAfterburnerCapability(JetNozzle nozzle, out bool hasAfterburner)
    {
        hasAfterburner = false;
        var field = JetNozzleAfterburners;
        if (field is null || !field.FieldType.IsArray)
        {
            return false;
        }

        if (field.GetValue(nozzle) is not Array afterburners)
        {
            return true;
        }

        for (var index = 0; index < afterburners.Length; index++)
        {
            if (afterburners.GetValue(index) is not null)
            {
                hasAfterburner = true;
                break;
            }
        }

        return true;
    }

    public static bool TryGetAfterburnerThrottleRanges(
        JetNozzle nozzle,
        List<AfterburnerThrottleRange> ranges)
    {
        ranges.Clear();
        var arrayField = JetNozzleAfterburners;
        var startField = JetNozzleAfterburnerThrottleStart;
        var endField = JetNozzleAfterburnerThrottleEnd;
        if (arrayField is null || startField is null || endField is null ||
            arrayField.GetValue(nozzle) is not Array afterburners)
        {
            return false;
        }

        for (var index = 0; index < afterburners.Length; index++)
        {
            var afterburner = afterburners.GetValue(index);
            if (afterburner is null)
            {
                continue;
            }

            ranges.Add(new AfterburnerThrottleRange(
                (float)startField.GetValue(afterburner),
                (float)endField.GetValue(afterburner)));
        }

        return true;
    }

    public static string FormatMethod(MethodBase method)
    {
        var parameters = string.Join(", ", method.GetParameters().Select(parameter =>
            $"{parameter.ParameterType.FullName} {parameter.Name}"));
        var returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType.FullName : "System.Void";
        return $"{returnType} {method.DeclaringType?.FullName}::{method.Name}({parameters})";
    }

    private static void ValidateDirectControlMembers(bool readsThrottle, bool readsNegativeThrottle)
    {
        if (readsThrottle)
        {
            RequireDirectField(typeof(ControlInputs), "throttle", typeof(float), isStatic: false);
        }

        RequireDirectField(typeof(PlayerSettings), "throttleUseRelative", typeof(bool), isStatic: true);
        RequireDirectField(typeof(PlayerSettings), "invertCollective", typeof(bool), isStatic: true);
        RequireDirectField(typeof(GameManager), "flightControlsEnabled", typeof(bool), isStatic: true);
        if (readsNegativeThrottle)
        {
            RequireDirectField(typeof(PlayerSettings), "throttleUseNegative", typeof(bool), isStatic: true);
        }
    }

    private static void RequireDirectField(Type type, string name, Type expectedType, bool isStatic)
    {
        var field = RequireDeclaredField(type, name, expectedType);
        if (field.IsStatic != isStatic)
        {
            throw new InvalidOperationException(
                $"Field {field.DeclaringType?.FullName}.{field.Name} has the wrong static/instance shape.");
        }
    }

    private static FieldInfo RequireDeclaredField(Type type, string name, Type expectedType)
    {
        var field = AccessTools.DeclaredField(type, name) ??
            throw new MissingFieldException(type.FullName, name);
        ValidateField(field, expectedType);
        return field;
    }

    private static FieldInfo RequireField(Type type, string name, Type expectedType)
    {
        var field = AccessTools.Field(type, name) ??
            throw new MissingFieldException(type.FullName, name);
        ValidateField(field, expectedType);
        return field;
    }

    private static void ValidateField(FieldInfo field, Type expectedType)
    {
        if (field.FieldType != expectedType)
        {
            throw new InvalidOperationException(
                $"Field {field.DeclaringType?.FullName}.{field.Name} is {field.FieldType.FullName}; expected {expectedType.FullName}.");
        }
    }

}

internal readonly struct AfterburnerThrottleRange
{
    public AfterburnerThrottleRange(float start, float end)
    {
        (Start, End) = (start, end);
    }

    public float Start { get; }
    public float End { get; }
}
