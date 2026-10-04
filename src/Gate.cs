using System;
using HarmonyLib;

namespace Svimma
{
    /// <summary>
    /// The decisions, kept apart from the patches that ask for them so the patches stay a list
    /// of where the game was changed and this stays a list of what the answer is.
    ///
    /// Everything answers for the local player only. Equipment on another player's character
    /// arrives through its ZDO, and that player's own client has already decided what they may
    /// hold, so a rule applied here to somebody else's body would only fight their client.
    /// </summary>
    internal static class Gate
    {
        /// <summary>
        /// The item Humanoid.EquipItem is working on, set by its prefix. The swimming test sits
        /// deep inside that method where the item is not on the stack, so the transpiled test
        /// reads it from here instead. Cleared by a finalizer, so a throw cannot leave one behind.
        /// </summary>
        internal static ItemDrop.ItemData Equipping;

        /// <summary>The last swing's price and what vanilla would have charged, for the console command.</summary>
        internal static float LastPaid = -1f;
        internal static float LastVanilla = -1f;

        private static AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> _hiddenLeft;
        private static AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> _hiddenRight;
        private static AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> _left;
        private static AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> _right;
        private static bool _handsBound;

        /// <summary>The item in the right hand, or null. GetRightItem is protected.</summary>
        internal static ItemDrop.ItemData Right(Humanoid humanoid)
        {
            return BindHands() ? _right(humanoid) : null;
        }

        /// <summary>The item in the left hand, or null. GetLeftItem is protected.</summary>
        internal static ItemDrop.ItemData Left(Humanoid humanoid)
        {
            return BindHands() ? _left(humanoid) : null;
        }

        /// <summary>
        /// Swimming with no ground under you, which is vanilla's own test in every place it puts
        /// your hands away. Wading is not this: both halves of the test need you off the ground.
        /// </summary>
        internal static bool Deep(Character character)
        {
            return character != null && character.IsSwimming() && !character.IsOnGround();
        }

        internal static bool Active(Character character)
        {
            return SvimmaConfig.Enabled.Value
                && Player.m_localPlayer != null
                && character == Player.m_localPlayer;
        }

        /// <summary>
        /// Replaces <c>IsSwimming()</c> in Humanoid.UpdateEquipment, whose next word is
        /// <c>HideHandItems()</c>. True keeps vanilla's behaviour; false leaves your hands alone.
        /// Both hands must be allowed: hiding only one would leave a shield out and a bow away,
        /// which is a half-state vanilla has no animation for.
        /// </summary>
        internal static bool HidesHands(Character character)
        {
            if (!character.IsSwimming()) return false;
            if (!Active(character)) return true;

            var humanoid = character as Humanoid;
            if (humanoid == null || !BindHands()) return true;

            bool stay = Kinds.Allowed(_right(humanoid)) && Kinds.Allowed(_left(humanoid));

            if (stay && SvimmaConfig.Verbose.Value && Deep(character))
                SvimmaPlugin.Log.LogInfo("Deep water: hands stay out ("
                    + Kinds.Name(_right(humanoid)) + ", " + Kinds.Name(_left(humanoid)) + ").");

            return !stay;
        }

        /// <summary>
        /// Replaces <c>IsSwimming()</c> in Humanoid.EquipItem, which refuses to equip anything
        /// for a swimming player. True keeps the refusal. Only the item named by the prefix is
        /// let through, so armour, food and everything else that vanilla refuses in the water
        /// is still refused.
        /// </summary>
        internal static bool BlocksEquip(Character character)
        {
            if (!character.IsSwimming()) return false;
            if (!Active(character)) return true;
            if (Equipping == null) return true;

            bool allowed = Kinds.Allowed(Equipping);

            if (SvimmaConfig.Verbose.Value && Deep(character))
                SvimmaPlugin.Log.LogInfo("Deep water: equip " + Kinds.Name(Equipping)
                    + (allowed ? " allowed." : " refused."));

            return !allowed;
        }

        /// <summary>
        /// Replaces <c>IsSwimming()</c> in Player.Update, in front of the line that draws hidden
        /// hand items again when the hide key is pressed. Without it a player who put a sword
        /// away in the water could never take it out until they were on land. The hidden items
        /// are read here because ShowHandItems clears them before it equips, so a refused item
        /// would be lost from both hand and hiding; if either is not allowed, vanilla stands.
        /// </summary>
        internal static bool BlocksShowHands(Character character)
        {
            if (!character.IsSwimming()) return false;
            if (!Active(character)) return true;

            var humanoid = character as Humanoid;
            if (humanoid == null) return true;

            if (!BindHands()) return true;

            return !(Kinds.Allowed(_hiddenLeft(humanoid)) && Kinds.Allowed(_hiddenRight(humanoid)));
        }

        /// <summary>
        /// Bound on first use, never in a static initializer. A throwing static initializer
        /// poisons every patch its class carries, and a renamed private field would then break
        /// unrelated vanilla features rather than only this one. On a failed bind every caller
        /// answers as vanilla does.
        /// </summary>
        private static bool BindHands()
        {
            if (_handsBound) return _right != null;
            _handsBound = true;

            try
            {
                _hiddenLeft = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_hiddenLeftItem");
                _hiddenRight = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_hiddenRightItem");
                _left = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_leftItem");
                _right = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");
            }
            catch (Exception error)
            {
                _hiddenLeft = null;
                _hiddenRight = null;
                _left = null;
                _right = null;
                SvimmaPlugin.Log.LogWarning("Could not read the hand items, so swimming puts them "
                    + "away as vanilla does: " + error.Message);
            }

            return _right != null;
        }
    }
}
