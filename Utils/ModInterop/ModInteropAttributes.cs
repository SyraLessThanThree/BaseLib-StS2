using System;

namespace BaseLib.Utils.ModInterop;

/// <summary>
/// Will patch methods, properties and classes in this class to reference methods/properties/fields/classes from another mod,
/// while avoiding a hard dependency.
/// Classes should inherit from InteropClassWrapper.
/// Methods should match the signature of the target method. If a method is static and its target is not, the instance
/// should be the first parameter.
/// </summary>
/// <param name="modId">The mod ID that must be loaded for this interop to be initialized.</param>
/// <param name="type">Will be used as base type for all members of the class that do not set type themselves.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModInteropAttribute(string modId, string? type = null) : Attribute
{
    public string ModId { get; } = modId;
    public string? Type { get; } = type;
}

/// <summary>
/// Type must be provided in this attribute or in the containing class's ModInteropAttribute.
/// If name is not provided, the name of the attached member will be used.
/// If targeting a class, Type and Name function identically.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class InteropTargetAttribute : Attribute
{
    public string? Type { get; }
    public string? Name { get; }

    public InteropTargetAttribute(string type, string? name = null)
    {
        Type = type;
        Name = name;
    }

    public InteropTargetAttribute(string? name = null)
    {
        Name = name;
    }
}