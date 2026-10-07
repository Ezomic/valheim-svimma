using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Ezomic.Core;
using HarmonyLib;

namespace Svimma
{
    /// <summary>
    /// Svimma. Swimming no longer puts the item in your hand away: a weapon, a pickaxe or a
    /// fishing rod stays out, and can be taken out, while you are in deep water.
    ///
    /// Vanilla hides your hand items every frame while you swim with no ground under you, and
    /// refuses to equip anything in that state, so the water is a place where you can only
    /// punch. Svimma lifts exactly those two checks and, for the hand items the player has
    /// allowed, nothing more. It is deliberately narrower than "use anything anywhere": the
    /// ranged weapons, staffs and building tools stay put away unless switched on, a swing
    /// costs more because stamina does not come back while you swim, and blocking is off.
    ///
    /// Where the work happens: all of it on the owning client, off state it already has. The
    /// game's own checks run there, the equipment other players see travels through the
    /// character's ZDO as it always did, and nothing is registered, spawned or saved. So it is
    /// client-side in the full sense and Requirement.HostOnly below is correct, not lazy.
    ///
    /// There is deliberately no BepInProcess attribute. A dedicated server runs
    /// valheim_server.exe, and Core's gate only refuses on the server side of RPC_PeerInfo -
    /// so a mod that must be enforced has to be allowed to load there.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Soft, not hard. A hard dependency that is absent does not degrade - the plugin never
    // loads at all - and every mod here has to be installable on its own, because a stranger
    // should not need two installs to get one mod. Soft still buys the load-order guarantee
    // when Core is present, which is all that registering with the gate needs.
    [BepInDependency(CoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class SvimmaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.svimma";
        public const string PluginName = "Svimma";
        public const string PluginVersion = "1.0.0";
        public const string PluginAuthor = "Robbin Thijssen";

        /// <summary>Core's plugin GUID. Optional - see TryRegisterWithCore.</summary>
        private const string CoreGuid = "ezomic.valheim.core";

        internal static ManualLogSource Log;

        /// <summary>
        /// Whether Core answered at load. Worth keeping even when nothing reads it yet: the
        /// difference between gated and ungated is invisible to a player otherwise, and this
        /// is what a warning on spawn would be driven by.
        /// </summary>
        internal static bool CorePresent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config first. Registering absorbs every entry the mod has bound, so anything
            // bound after this line is carried only because Core re-absorbs at manifest
            // time - and depending on the order of two lines in an Awake is not a thing
            // worth relying on.
            SvimmaConfig.Bind(Config);

            TryRegisterWithCore();

            // PatchAll over a named type, never the whole assembly. A bare PatchAll() walks
            // every type in the DLL, so a half-written patch class in another file goes live
            // the moment it compiles.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SvimmaPatches));
            _harmony.PatchAll(typeof(HoldPatch));
            _harmony.PatchAll(typeof(EquipPatch));
            _harmony.PatchAll(typeof(DrawPatch));
            _harmony.PatchAll(typeof(BlockPatch));
            _harmony.PatchAll(typeof(CostPatch));
            _harmony.PatchAll(typeof(DevConsole.Hook));

            // The startup line every mod in the suite writes. It is how a log answers "which
            // build of what is actually loaded" without anyone guessing.
            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        /// <summary>
        /// Joins Core's version gate when Core is installed, and does nothing when it is not.
        ///
        /// What standing without Core costs here is the host's say. Nothing then imposes the
        /// host's allowed list, price or blocking rule on a client, so those become an agreement
        /// between players rather than a property of the server. That is the server owner's to
        /// take, which is why this logs rather than refusing to run.
        /// </summary>
        private void TryRegisterWithCore()
        {
            CorePresent = Chainloader.PluginInfos.ContainsKey(CoreGuid);

            if (!CorePresent)
            {
                Log.LogInfo("Core not installed - running standalone, without the version gate.");
                return;
            }

            RegisterWithCore();
        }

        /// <summary>
        /// Kept separate and never inlined on purpose. The JIT resolves the assemblies a
        /// method needs when it first compiles that method, so a Suite call sitting directly
        /// in Awake would drag Ezomic.Core in before the check above could prevent it - and
        /// the missing-assembly exception would land during plugin load, which is the exact
        /// failure this arrangement exists to avoid. Isolating it means the type is only
        /// ever resolved on a machine that has Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterWithCore()
        {
            // HostOnly, and it is load-bearing rather than lazy. Svimma registers no prefab,
            // changes no item data and writes no value of its own, so a player without it cannot
            // fail to resolve anything: they simply cannot swing in the water, and the swing a
            // player with it takes is shown to them through the ordinary equipment and animation
            // state every client already reads. Everyone would refuse those players the server for
            // no gain. Core treats HostOnly symmetrically, so a Svimma client can also still join
            // a server that does not run it and simply gets the rule it has locally.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config, Requirement.HostOnly);

            // Registering already absorbs the whole config file, so these are a formality. They
            // are still worth writing: naming an entry here is saying out loud that the host
            // decides it. What may be used in deep water, what a swing costs and whether you can
            // block are rules about a fight, and a table where everyone brought their own
            // AllowBows is not a table anybody agreed to sit at.
            Suite.Sync(SvimmaConfig.Enabled);
            Suite.Sync(SvimmaConfig.AllowMelee);
            Suite.Sync(SvimmaConfig.AllowPickaxes);
            Suite.Sync(SvimmaConfig.AllowFishing);
            Suite.Sync(SvimmaConfig.AllowBows);
            Suite.Sync(SvimmaConfig.AllowCrossbows);
            Suite.Sync(SvimmaConfig.AllowStaffs);
            Suite.Sync(SvimmaConfig.AllowBuildingTools);
            Suite.Sync(SvimmaConfig.DeepSwingStaminaMultiplier);
            Suite.Sync(SvimmaConfig.AllowBlocking);

            // The one personal setting. Registering syncs the whole config bar keybinds, so
            // Verbose would be put back by the host's value, and a host's logging is not
            // something to impose on somebody else's log file.
            Suite.Local(SvimmaConfig.Verbose);

            // If the mod reads a data file that decides what it does, hash it too. The gate
            // catches two ends on different builds; it cannot catch two ends running the
            // same build over different text unless it is told.
            //
            //     Suite.Data(File.ReadAllText(path));
        }

        private void OnDestroy()
        {
            // UnpatchSelf, never UnpatchAll(). The argumentless one unpatches every mod in
            // the process, not just this one.
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
