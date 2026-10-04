using System.Globalization;
using System.Text;
using HarmonyLib;

namespace Svimma
{
    /// <summary>
    /// `svimma` in the console, for a Devkit scenario that has to ask what the player is doing
    /// in the water and make the player do the three things the mod changes.
    ///
    ///   svimma               one line of state: swimming, deep, hands, blocking, stamina
    ///   svimma equip &lt;item&gt;   EquipItem on a carried item, and whether the game took it
    ///   svimma swing         StartAttack with the current weapon, and whether it started
    ///   svimma cost          what the last deep swing was charged beside what vanilla charges
    ///
    /// isCheat false, and honestly: each is something a player does with a key. None gives an
    /// item, changes a rule or skips a check, and the equip and the swing go through the same
    /// game methods the keys do, so they are refused by the same gates.
    ///
    /// Every answer is name=value with no spaces inside, so Devkit's `printed` step can match.
    /// </summary>
    internal static class DevConsole
    {
        /// <summary>
        /// Process-wide, not per world. Terminal's command table is a private static that nothing
        /// clears, so a second registration would leave a duplicate behind.
        /// </summary>
        private static bool _registered;

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        internal static class Hook
        {
            private static void Postfix()
            {
                if (_registered) return;
                _registered = true;

                new Terminal.ConsoleCommand("svimma",
                    "[equip <item> | swing | cost] - what you are doing in the water, and the three things "
                    + "Svimma changes there. With no word it reports",
                    OnCommand, isCheat: false);
            }
        }

        private static void OnCommand(Terminal.ConsoleEventArgs args)
        {
            Terminal term = args.Context;
            if (term == null) return;

            Player player = Player.m_localPlayer;
            if (player == null) { term.AddString("svimma: no player"); return; }

            string verb = args.Length > 1 ? args[1].ToLowerInvariant() : "";

            switch (verb)
            {
                case "":
                    term.AddString(State(player));
                    return;
                case "equip":
                    term.AddString(Equip(player, args.Length > 2 ? args[2] : ""));
                    return;
                case "swing":
                    term.AddString(Swing(player));
                    return;
                case "cost":
                    term.AddString(Cost());
                    return;
                default:
                    term.AddString("svimma: unknown '" + args[1] + "'. Try svimma, svimma equip <item>, svimma swing, svimma cost.");
                    return;
            }
        }

        private static string State(Player player)
        {
            var text = new StringBuilder("svimma");
            text.Append(" enabled=").Append(Word(SvimmaConfig.Enabled.Value));
            text.Append(" swimming=").Append(Word(player.IsSwimming()));
            text.Append(" deep=").Append(Word(Gate.Deep(player)));
            text.Append(" right=").Append(Kinds.Name(Gate.Right(player)));
            text.Append(" left=").Append(Kinds.Name(Gate.Left(player)));
            text.Append(" blocking=").Append(Word(player.IsBlocking()));
            text.Append(" stamina=").Append(player.GetStamina().ToString("0.0", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        private static string Equip(Player player, string prefab)
        {
            if (prefab.Length == 0) return "svimma equip: which item? (a prefab name, like SwordBronze)";

            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (item == null || item.m_dropPrefab == null) continue;
                if (!string.Equals(item.m_dropPrefab.name, prefab, System.StringComparison.OrdinalIgnoreCase)) continue;

                if (item.m_equipped) return "svimma equip=already item=" + item.m_dropPrefab.name;

                bool took = player.EquipItem(item);
                return "svimma equip=" + (took ? "ok" : "refused") + " item=" + item.m_dropPrefab.name;
            }

            return "svimma equip=missing item=" + prefab + " (give it first)";
        }

        private static string Swing(Player player)
        {
            Gate.LastPaid = -1f;
            Gate.LastVanilla = -1f;

            bool started = player.StartAttack(null, false);
            return "svimma swing=" + (started ? "started" : "refused")
                + " stamina=" + player.GetStamina().ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string Cost()
        {
            if (Gate.LastPaid < 0f) return "svimma cost=none (no deep swing has been priced since the last svimma swing)";

            float ratio = Gate.LastVanilla > 0f ? Gate.LastPaid / Gate.LastVanilla : 0f;
            return "svimma cost=" + Gate.LastPaid.ToString("0.00", CultureInfo.InvariantCulture)
                + " vanilla=" + Gate.LastVanilla.ToString("0.00", CultureInfo.InvariantCulture)
                + " ratio=" + ratio.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Word(bool value)
        {
            return value ? "yes" : "no";
        }
    }
}
