using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Utils;

[AttributeUsage(AttributeTargets.Field)]
public class SavedSpireFieldAttribute() : Attribute { }

static class SavedSpireField
{
    private static readonly List<ISavedField> RegisteredFields = [];
    private static readonly HashSet<Assembly> ScannedAssemblies = [];

    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(int),
        typeof(bool),
        typeof(string),
        typeof(ModelId),
        typeof(int[]),
        typeof(SerializableCard),
        typeof(SerializableCard[]),
        typeof(List<SerializableCard>),
    ];

    private interface ISavedField
    {
        string Name { get; }
        Type TargetType { get; }
        void Export(object model, SavedProperties props);
        void Import(object model, SavedProperties props);
    }

    private static void ScanAndInject(Assembly assembly)
    {
        if (!ScannedAssemblies.Add(assembly))
            return;

        foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
        {
            var fields = type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                )
                .Where(f => f.GetCustomAttribute<SavedSpireFieldAttribute>() != null);

            foreach (var field in fields)
            {
                Type fType = field.FieldType;

                if (
                    !fType.IsGenericType
                    || fType.GetGenericTypeDefinition() != typeof(SpireField<,>)
                )
                {
                    throw new Exception(
                        $"[SavedSpireField] on {type.Name}.{field.Name} must be a SpireField<,>."
                    );
                }

                Type tKey = fType.GetGenericArguments()[0];
                Type tVal = fType.GetGenericArguments()[1];

                if (!IsTypeSupported(tVal))
                {
                    throw new NotSupportedException(
                        $"[SavedSpireField] on {type.Name}.{field.Name} uses unsupported type {tVal.Name}."
                    );
                }

                InjectNameIntoBaseGameCache(field.Name);

                object? val = field.GetValue(null);
                if (val == null)
                    continue;

                var method = AccessTools.Method(typeof(SavedSpireField), nameof(Register));
                method.MakeGenericMethod(tKey, tVal).Invoke(null, [field.Name, val]);
            }
        }
    }

    private static bool IsTypeSupported(Type t) =>
        SupportedTypes.Contains(t) || t.IsEnum || (t.IsArray && t.GetElementType()!.IsEnum);

    private static void InjectNameIntoBaseGameCache(string name)
    {
        ref var propertyToId = ref AccessTools.StaticFieldRefAccess<Dictionary<string, int>>(
            typeof(SavedPropertiesTypeCache),
            "_propertyNameToNetIdMap"
        );
        ref var idToProperty = ref AccessTools.StaticFieldRefAccess<List<string>>(
            typeof(SavedPropertiesTypeCache),
            "_netIdToPropertyNameMap"
        );

        if (!propertyToId.ContainsKey(name))
        {
            propertyToId[name] = idToProperty.Count;
            idToProperty.Add(name);

            int newBitSize = (int)Math.Ceiling(Math.Log2(idToProperty.Count));

            AccessTools
                .Property(typeof(SavedPropertiesTypeCache), "NetIdBitSize")
                .SetValue(null, newBitSize);
        }
    }

    private static void Register<TKey, TVal>(string name, SpireField<TKey, TVal> field)
        where TKey : class => RegisteredFields.Add(new SavedFieldImpl<TKey, TVal>(name, field));

    private static IEnumerable<ISavedField> GetFieldsForModel(object model) =>
        RegisteredFields.Where(f => f.TargetType.IsInstanceOfType(model));

    private class SavedFieldImpl<TKey, TVal>(string name, SpireField<TKey, TVal> field)
        : ISavedField
        where TKey : class
    {
        public string Name { get; } = name;
        public Type TargetType => typeof(TKey);

        public void Export(object model, SavedProperties props) =>
            AddToProperties(props, Name, field.Get((TKey)model));

        public void Import(object model, SavedProperties props)
        {
            if (TryGetFromProperties<TVal>(props, Name, out var val))
                field.Set((TKey)model, val);
        }
    }

    private static void AddToProperties(SavedProperties props, string name, object? value)
    {
        if (value == null)
            return;

        if (value is int i)
            (props.ints ??= []).Add(new(name, i));
        else if (value is bool b)
            (props.bools ??= []).Add(new(name, b));
        else if (value is string s)
            (props.strings ??= []).Add(new(name, s));
        else if (value is Enum e)
            (props.ints ??= []).Add(new(name, Convert.ToInt32(e)));
        else if (value is ModelId mid)
            (props.modelIds ??= []).Add(new(name, mid));
        else if (value is SerializableCard card)
            (props.cards ??= []).Add(new(name, card));
        else if (value is int[] iArr)
            (props.intArrays ??= []).Add(new(name, iArr));
        else if (value is Enum[] eArr)
            (props.intArrays ??= []).Add(new(name, eArr.Select(Convert.ToInt32).ToArray()));
        else if (value is SerializableCard[] cArr)
            (props.cardArrays ??= []).Add(new(name, cArr));
        else if (value is List<SerializableCard> cList)
            (props.cardArrays ??= []).Add(new(name, cList.ToArray()));
    }

    private static bool TryGetFromProperties<T>(SavedProperties props, string name, out T? value)
    {
        value = default;

        if (typeof(T) == typeof(int) || typeof(T).IsEnum)
        {
            var found = props.ints?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                value = typeof(T).IsEnum
                    ? (T)Enum.ToObject(typeof(T), found.Value.value)
                    : (T)(object)found.Value.value;
                return true;
            }
        }
        else if (typeof(T) == typeof(bool))
        {
            var found = props.bools?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                value = (T)(object)found.Value.value;
                return true;
            }
        }
        else if (typeof(T) == typeof(string))
        {
            var found = props.strings?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                value = (T)(object)found.Value.value;
                return true;
            }
        }
        else if (typeof(T) == typeof(ModelId))
        {
            var found = props.modelIds?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                value = (T)(object)found.Value.value;
                return true;
            }
        }
        else if (
            typeof(T) == typeof(int[])
            || (typeof(T).IsArray && typeof(T).GetElementType()!.IsEnum)
        )
        {
            var found = props.intArrays?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                if (typeof(T).IsArray && typeof(T).GetElementType()!.IsEnum)
                {
                    Type enumType = typeof(T).GetElementType()!;
                    Array enumArr = Array.CreateInstance(enumType, found.Value.value.Length);
                    for (int i = 0; i < found.Value.value.Length; i++)
                        enumArr.SetValue(Enum.ToObject(enumType, found.Value.value[i]), i);
                    value = (T)(object)enumArr;
                }
                else
                {
                    value = (T)(object)found.Value.value;
                }
                return true;
            }
        }
        else if (typeof(T) == typeof(SerializableCard))
        {
            var found = props.cards?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                value = (T)(object)found.Value.value;
                return true;
            }
        }
        else if (
            typeof(T) == typeof(SerializableCard[])
            || typeof(T) == typeof(List<SerializableCard>)
        )
        {
            var found = props.cardArrays?.FirstOrDefault(p => p.name == name);
            if (found?.name != null)
            {
                value =
                    typeof(T) == typeof(List<SerializableCard>)
                        ? (T)(object)found.Value.value.ToList()
                        : (T)(object)found.Value.value;
                return true;
            }
        }
        return false;
    }

    [HarmonyPatch(typeof(SavedProperties))]
    private static class SavedPropertiesPatch
    {
        [HarmonyPatch(nameof(SavedProperties.FromInternal))]
        [HarmonyPostfix]
        static void PostfixFromInternal(ref SavedProperties? __result, object model)
        {
            if (model == null)
                return;
            var props = __result ?? new SavedProperties();
            bool added = false;
            foreach (var field in GetFieldsForModel(model))
            {
                field.Export(model, props);
                added = true;
            }
            if (__result == null && added)
                __result = props;
        }

        [HarmonyPatch(nameof(SavedProperties.FillInternal))]
        [HarmonyPostfix]
        static void PostfixFillInternal(SavedProperties __instance, object model)
        {
            if (model == null || __instance == null)
                return;
            foreach (var field in GetFieldsForModel(model))
                field.Import(model, __instance);
        }
    }

    [HarmonyPatch(typeof(SavedPropertiesTypeCache))]
    private static class AutoScanPatch
    {
        [HarmonyPatch(MethodType.StaticConstructor)]
        static void Postfix()
        {
            string? currentAssemblyName = typeof(SavedSpireField).Assembly.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                if (assembly.GetReferencedAssemblies().Any(a => a.Name == currentAssemblyName))
                {
                    ScanAndInject(assembly);
                }
            }
        }
    }
}
