using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.Content
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
    class CustomAnimationPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(NCreature __instance, string trigger)
        {
            if (__instance.HasSpineAnimation) return true;

            var animPlayer = FindAnimationPlayer(__instance.Visuals);
            if (animPlayer == null) return false;

            var animName = trigger switch
            {
                CreatureAnimator.idleTrigger => "idle",
                CreatureAnimator.attackTrigger => "attack",
                CreatureAnimator.castTrigger => "cast",
                CreatureAnimator.hitTrigger => "hurt",
                CreatureAnimator.deathTrigger => "die",
                _ => trigger.ToLowerInvariant()
            };

            if (animPlayer.HasAnimation(animName))
                animPlayer.Play(animName);
            else if (animPlayer.HasAnimation(trigger))
                animPlayer.Play(trigger);

            return false;
        }

        private static AnimationPlayer? FindAnimationPlayer(Node root)
        {
            return root.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?? root.GetNodeOrNull<AnimationPlayer>("Visuals/AnimationPlayer")
                ?? root.GetNodeOrNull<AnimationPlayer>("Body/AnimationPlayer")
                ?? SearchRecursive(root);
        }

        private static AnimationPlayer? SearchRecursive(Node node)
        {
            foreach (var child in node.GetChildren())
            {
                if (child is AnimationPlayer player) return player;
                var found = SearchRecursive(child);
                if (found != null) return found;
            }
            return null;
        }

    }
}
