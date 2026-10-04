using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Svimma
{
    /// <summary>
    /// Where the game is changed, and nothing else. The three places vanilla puts your hands
    /// away in the water were found by reading the decompile, not by guessing:
    ///
    ///   Humanoid.UpdateEquipment  every frame:  if (IsSwimming() &amp;&amp; !IsOnGround()) HideHandItems();
    ///   Humanoid.EquipItem        refuses:      if (IsPlayer() &amp;&amp; !IsDead() &amp;&amp; IsSwimming() &amp;&amp; !IsOnGround()) return false;
    ///   Player.Update             the hide key: else if ((!IsSwimming() || IsOnGround()) &amp;&amp; !InDodge()) ShowHandItems();
    ///
    /// Each is one call to <c>Character.IsSwimming</c> inside a longer condition, so each is
    /// patched by a transpiler that swaps that one call for a question to <see cref="Gate"/>.
    /// A prefix could not do it: the condition is in the middle of the method, and skipping
    /// the method would skip the durability drain beside it. Patching IsSwimming itself was
    /// rejected because it is also the movement and animation state, and answering "not
    /// swimming" there would stand you up on the water.
    ///
    /// Nothing else blocks an attack in the water. StartAttack, Attack.Start and the attack
    /// input read no swimming state, which is why an unarmed swing already works there. What
    /// the animator does with the swing is not in the code and is the thing to watch in game.
    /// </summary>
    internal static class SvimmaPatches
    {
        /// <summary>
        /// A patch that does nothing, kept so the wiring is proved rather than assumed. If this
        /// line is absent from the log, the problem is the patch not applying, not the logic.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static void OnSpawned(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            if (!SvimmaConfig.Enabled.Value || !SvimmaConfig.Verbose.Value) return;

            SvimmaPlugin.Log.LogInfo("Player spawned - patches are live.");
        }
    }

    /// <summary>Swaps each <c>Character.IsSwimming</c> call in a method for a call to a Gate method.</summary>
    internal static class Swap
    {
        private static readonly MethodInfo IsSwimming = AccessTools.Method(typeof(Character), nameof(Character.IsSwimming));

        /// <summary>
        /// Counts what it replaced and says so at patch time. A game update that moves the call
        /// makes the count wrong, and a patch that silently replaces nothing is a mod that
        /// silently does nothing, which reads as a config problem.
        /// </summary>
        internal static List<CodeInstruction> Replace(
            IEnumerable<CodeInstruction> code, string replacement, string where, int expected)
        {
            MethodInfo gate = AccessTools.Method(typeof(Gate), replacement);
            var list = new List<CodeInstruction>(code);
            int found = 0;

            foreach (CodeInstruction instruction in list)
            {
                if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt) continue;
                var called = instruction.operand as MethodInfo;
                if (called == null || called != IsSwimming) continue;

                instruction.opcode = OpCodes.Call;
                instruction.operand = gate;
                found++;
            }

            if (found == expected)
                SvimmaPlugin.Log.LogInfo(where + ": replaced " + found + " IsSwimming call(s) with Gate." + replacement + ".");
            else
                SvimmaPlugin.Log.LogWarning(where + ": expected to replace " + expected
                    + " IsSwimming call(s) and replaced " + found
                    + ". The game has changed here and this part of Svimma does nothing.");

            return list;
        }
    }

    [HarmonyPatch(typeof(Humanoid), "UpdateEquipment")]
    internal static class HoldPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            return Swap.Replace(code, nameof(Gate.HidesHands), "Humanoid.UpdateEquipment", 1);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class EquipPatch
    {
        private static void Prefix(ItemDrop.ItemData item)
        {
            Gate.Equipping = item;
        }

        // A finalizer rather than a postfix: it runs when the method throws as well, and it
        // takes no exception parameter, so it does not swallow one either.
        private static void Finalizer()
        {
            Gate.Equipping = null;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            return Swap.Replace(code, nameof(Gate.BlocksEquip), "Humanoid.EquipItem", 1);
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class DrawPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            return Swap.Replace(code, nameof(Gate.BlocksShowHands), "Player.Update", 1);
        }
    }

    /// <summary>
    /// Blocking off in deep water unless AllowBlocking. Patched on the result rather than on
    /// the input, so the block animation and the blocking flag other players see are never
    /// started, and nothing downstream (BlockAttack, the stamina regen halving) believes in a
    /// block that is not happening. Only the owner's own value is changed: for anybody else
    /// IsBlocking reads the ZDO.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
    internal static class BlockPatch
    {
        private static void Postfix(Humanoid __instance, ref bool __result)
        {
            if (!__result || SvimmaConfig.AllowBlocking.Value) return;
            if (!Gate.Active(__instance) || !Gate.Deep(__instance)) return;

            __result = false;
        }
    }

    /// <summary>
    /// The price of a swing in deep water. Attack.GetAttackStamina is asked twice per swing,
    /// once to check you can afford it (Attack.Start) and once to charge you (OnAttackTrigger),
    /// so patching it keeps the two in step: a swing you cannot pay the raised price for is
    /// refused, rather than started and then charged more than you had.
    /// </summary>
    [HarmonyPatch(typeof(Attack), "GetAttackStamina")]
    internal static class CostPatch
    {
        private static AccessTools.FieldRef<Attack, Humanoid> _owner;
        private static bool _bound;

        private static void Postfix(Attack __instance, ref float __result)
        {
            if (__result <= 0f) return;

            float multiplier = SvimmaConfig.DeepSwingStaminaMultiplier.Value;
            if (multiplier == 1f || !Bind()) return;

            Humanoid owner = _owner(__instance);
            if (owner == null || !Gate.Active(owner) || !Gate.Deep(owner)) return;

            Gate.LastVanilla = __result;
            __result *= multiplier;
            Gate.LastPaid = __result;

            if (SvimmaConfig.Verbose.Value)
                SvimmaPlugin.Log.LogInfo("Deep swing: vanilla " + Gate.LastVanilla.ToString("0.0")
                    + " stamina, charged " + Gate.LastPaid.ToString("0.0") + ".");
        }

        private static bool Bind()
        {
            if (_bound) return _owner != null;
            _bound = true;

            try { _owner = AccessTools.FieldRefAccess<Attack, Humanoid>("m_character"); }
            catch (Exception error)
            {
                _owner = null;
                SvimmaPlugin.Log.LogWarning("Could not read who is swinging, so a deep swing "
                    + "costs the land price: " + error.Message);
            }

            return _owner != null;
        }
    }
}
