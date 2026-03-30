using System.Collections.Generic;
using HarmonyLib;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomPotionModel : PotionModel, ICustomModel, ILocalizationProvider
{
    public virtual bool AutoAdd => true;
    public CustomPotionModel()
    {
        if (AutoAdd) CustomContentDictionary.AddModel(GetType());
    }

    public virtual string? PackedImagePath => null;
    public virtual string? PackedOutlinePath => null;
    
    /// <summary>
    /// Override this to define localization directly in your class.
    /// You are recommended to return a PotionLoc<seealso cref="PotionLoc"/>.
    /// </summary>
    public virtual List<(string, string)>? Localization => null;
    
    [HarmonyPatch(typeof(PotionModel), nameof(CustomPotionModel.PackedImagePath), MethodType.Getter)]
    private static class ImagePatch {
        static bool Prefix(PotionModel __instance, ref string __result) {
            if (__instance is not CustomPotionModel model || model.PackedImagePath is not string path)
                return true;
            __result = path;
            return false;
        }
    }
    [HarmonyPatch(typeof(PotionModel), nameof(CustomPotionModel.PackedOutlinePath), MethodType.Getter)]
    private static class OutlinePatch {
        static bool Prefix(PotionModel __instance, ref string __result) {
            if (__instance is not CustomPotionModel model || model.PackedOutlinePath is not string path)
                return true;
            __result = path;
            return false;
        }
    }
}
