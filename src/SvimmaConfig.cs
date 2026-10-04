using BepInEx.Configuration;

namespace Svimma
{
    /// <summary>
    /// Everything tunable, bound in one place so the .cfg reads as a document.
    ///
    /// The standing BepInEx trap applies: every entry is written on first run and the saved
    /// value beats a new default in code. Edit
    /// <c>&lt;profile&gt;\BepInEx\config\ezomic.valheim.svimma.cfg</c> as part of any change to
    /// a default here.
    ///
    /// Every switch except Verbose is a rule rather than a preference, so Core imposes the
    /// host's value on a client of that host. See SvimmaPlugin.RegisterWithCore.
    /// </summary>
    internal static class SvimmaConfig
    {
        internal static ConfigEntry<bool> Enabled;

        internal static ConfigEntry<bool> AllowMelee;
        internal static ConfigEntry<bool> AllowPickaxes;
        internal static ConfigEntry<bool> AllowFishing;
        internal static ConfigEntry<bool> AllowBows;
        internal static ConfigEntry<bool> AllowCrossbows;
        internal static ConfigEntry<bool> AllowStaffs;
        internal static ConfigEntry<bool> AllowBuildingTools;

        internal static ConfigEntry<float> DeepSwingStaminaMultiplier;
        internal static ConfigEntry<bool> AllowBlocking;

        internal static ConfigEntry<bool> Verbose;

        internal static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("Svimma", "Enabled", true,
                "Off leaves the plugin loaded and changing nothing: swimming puts your hands away "
                + "exactly as vanilla does.");

            // The default list is the point of the mod. A generic "use anything anywhere" would be
            // wider than what was asked for and would make the water a place where every weapon
            // class works the same as on land. These seven are separate switches so a rejected
            // default is one line to flip rather than a rebuild.
            AllowMelee = cfg.Bind("What may be used", "AllowMelee", true,
                "Swords, knives, clubs, spears, polearms, axes and anything else swung or thrust "
                + "that is not claimed by one of the switches below. Shields stay in hand with "
                + "whatever else is allowed. Torches never do: a flame is put out the way vanilla "
                + "puts it away, and there is no switch for that.");

            AllowPickaxes = cfg.Bind("What may be used", "AllowPickaxes", true,
                "Pickaxes, for deposits you can reach from the water but not stand beside.");

            AllowFishing = cfg.Bind("What may be used", "AllowFishing", true,
                "The fishing rod. Fishing from the water is the case the ticket was written "
                + "around: the rod is a tool, not a weapon, and there is nothing in it that wants "
                + "a dry foot.");

            // Off by default, and not because they do not work. A ranged attack out of the water
            // is a different fight from a melee one: you can shoot a shore you cannot reach. Narrower
            // than the thing it replaces is the rule for every quality of life mod here.
            AllowBows = cfg.Bind("What may be used", "AllowBows", false,
                "Bows. Off by default: a bow in deep water lets you fight anything on shore with "
                + "no cost for being out of reach, which is a bigger change than swinging a sword.");

            AllowCrossbows = cfg.Bind("What may be used", "AllowCrossbows", false,
                "Crossbows. Off for the same reason as bows, and separately because a crossbow's "
                + "reload is a second action in a state that is already draining stamina.");

            AllowStaffs = cfg.Bind("What may be used", "AllowStaffs", false,
                "Staffs (elemental and blood magic). Off by default: they spend eitr, not "
                + "stamina, so the swim cost below does not touch them, and that would make them "
                + "free to use in a place everything else is made to pay.");

            AllowBuildingTools = cfg.Bind("What may be used", "AllowBuildingTools", false,
                "The hammer, the hoe and the cultivator. Off by default: building on water you "
                + "are swimming in puts pieces and terrain changes where you could not have "
                + "stood to make them. Turn it on only if that is what you want.");

            // A multiplier rather than a flat extra, so it keeps the spread between a bronze
            // swing and a flametal one. 1.5 because stamina does not come back while you swim
            // (Player.UpdateStats sets the regen to zero), so a swing in deep water is paid for
            // out of a bar that stays spent until you leave. 1 makes it free relative to land,
            // which the ticket's own question about cost was written to avoid.
            DeepSwingStaminaMultiplier = cfg.Bind("Cost", "DeepSwingStaminaMultiplier", 1.5f,
                new ConfigDescription(
                    "What a swing costs in stamina while you are swimming in deep water, as a "
                    + "multiple of what the same swing costs on land. 1 = no extra. Wading is not "
                    + "deep water: standing on the bottom, even up to the neck, costs the land "
                    + "price. Remember an empty bar while swimming is drowning, and a swing is "
                    + "refused when you cannot pay for it, exactly as on land.",
                    new AcceptableValueRange<float>(1f, 5f)));

            AllowBlocking = cfg.Bind("What may be used", "AllowBlocking", false,
                "Off: blocking is switched off while you swim in deep water, so a shield in hand "
                + "is carried and does not block. On: blocking works as on land. Off by default "
                + "because you cannot plant your feet in the water, and a block that cannot cost "
                + "you footing is a block that costs nothing.");

            // Not synced by intent. A diagnostic flag is personal, and a host turning on someone
            // else's logging is not a thing anybody asked for.
            Verbose = cfg.Bind("Svimma", "Verbose", false,
                "Write each decision to BepInEx/LogOutput.log: what was allowed to stay in hand "
                + "in deep water and what a swing cost. Off unless something looks wrong.");
        }
    }
}
