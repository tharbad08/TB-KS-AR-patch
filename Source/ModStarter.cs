using System;
using System.Linq;
using AcademicsRainborn;
using Bindito.Core;
using HarmonyLib;
using Keystone.Mod.Recipes;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.ModManagerScene;
using Timberborn.NaturalResources;
using Timberborn.SingletonSystem;
using Timberborn.TemplateSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace TBKSARPatch
{
    // Uses game/mod types directly. No constructor interception or runtime method discovery.
    public sealed class ModStarter : IModStarter
    {
        public void StartMod(IModEnvironment environment)
        {
            new Harmony("tharbad08.TBKSARPatch").PatchAll(typeof(ModStarter).Assembly);
            Debug.Log("[TB-KS-AR] v1.1.2 typed compatibility hooks installed.");
        }
    }

    [Context("Game")]
    public sealed class GameConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition container)
        {
            container.Bind<ExcavationCompatibilityService>().AsSingleton();
        }
    }

    // Bindito constructs this once per game scene; state is cleared when that scene unloads.
    public sealed class ExcavationCompatibilityService : ILoadableSingleton, IUnloadableSingleton
    {
        internal static ExcavationCompatibilityService Current { get; private set; }

        private readonly IBlockService _blocks;
        private readonly ITerrainService _terrain;
        private readonly EntityService _entities;
        private readonly TemplateNameMapper _templates;
        private BlockObjectSpec _flagSpec;
        private BlockObjectSpec _dynamiteSpec;
        private int _removed;
        private bool _warnedMissingFlag;

        [ThreadStatic] private static bool _cleaning;

        public ExcavationCompatibilityService(
            IBlockService blocks, ITerrainService terrain, EntityService entities,
            TemplateNameMapper templates)
        {
            _blocks = blocks;
            _terrain = terrain;
            _entities = entities;
            _templates = templates;
        }

        public void Load()
        {
            _removed = 0;
            _flagSpec = ResolveSpec("RainbornExcavationFlag",
                "RainbornExcavationFlag.Folktails", "RainbornExcavationFlag.IronTeeth");
            _dynamiteSpec = ResolveSpec("Dynamite", "Dynamite.Folktails", "Dynamite.IronTeeth");
            Current = this;
            if (_flagSpec == null)
                Debug.LogWarning("[TB-KS-AR] Rainborn flag template not resolved; additional placement restrictions disabled, cleanup remains active.");
        }

        public void Unload()
        {
            if (ReferenceEquals(Current, this)) Current = null;
            _flagSpec = null;
            _dynamiteSpec = null;
        }

        private BlockObjectSpec ResolveSpec(params string[] names)
        {
            foreach (var name in names)
            {
                // The template mapper is a typed game API; alternatives may not exist in one faction.
                try
                {
                    var template = _templates.GetTemplate(name);
                    if (template != null)
                    {
                        var candidate = template.Blueprint.GetSpec<BlockObjectSpec>();
                        if (candidate != null) return candidate;
                    }
                }
                catch (Exception) { /* That template name is absent in this faction. */ }
            }
            return null;
        }

        internal bool IsRainbornSpec(BlockObjectSpec candidate)
        {
            // Never guess from a ToString or a class name. A mismatched spec is not patched.
            if (_flagSpec != null) return ReferenceEquals(candidate, _flagSpec);
            if (!_warnedMissingFlag)
            {
                _warnedMissingFlag = true;
                Debug.LogWarning("[TB-KS-AR] Placement parity cannot be enforced until Rainborn flag template resolves.");
            }
            return false;
        }

        internal bool ValidPlacement(BlockValidator validator, Placement placement)
        {
            var cell = placement.Coordinates;
            if (cell.z <= 0 || !_terrain.Contains(cell)) return false;
            var below = new Vector3Int(cell.x, cell.y, cell.z - 1);
            if (!_terrain.Underground(below)) return false;

            // Calling the normal validator preserves vanilla dynamite's occupancy rules.
            // Harmony's postfix skips this nested call using the thread-local guard below.
            return _dynamiteSpec == null || validator.BlocksValid(_dynamiteSpec, placement);
        }

        internal void CleanAtFlag(BlockObject flag, string stage)
        {
            if (flag == null || _cleaning) return;
            _cleaning = true;
            try
            {
                var cell = flag.Coordinates;
                if (cell.z <= 0 || !TerrainAbsentBelow(cell)) return;
                var above = new Vector3Int(cell.x, cell.y, cell.z + 1);
                var count = 0;
                foreach (var pos in new[] { cell, above })
                {
                    if (!_terrain.Contains(pos)) continue;
                    // Snapshot first: Delete() may update the underlying game collection.
                    foreach (var candidate in _blocks.GetObjectsAt(pos).ToArray())
                    {
                        if (candidate == null || ReferenceEquals(candidate, flag)) continue;
                        if (candidate.GetComponent<NaturalResource>() == null &&
                            candidate.GetComponent<KeystoneVariant>() == null) continue;
                        if (!IsGroundDependentAt(candidate, pos) || !TerrainAbsentBelow(pos)) continue;
                        try
                        {
                            _entities.Delete(candidate);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[TB-KS-AR] Natural-resource deletion failed: " + ex);
                        }
                    }
                }
                _removed += count;
                if (count > 0 && (_removed <= 10 || _removed % 50 == 0))
                    Debug.Log("[TB-KS-AR] Removed " + count + " unsupported natural resources during " +
                        stage + "; total=" + _removed);
            }
            finally { _cleaning = false; }
        }

        private bool TerrainAbsentBelow(Vector3Int pos)
        {
            if (pos.z <= 0) return false;
            var below = new Vector3Int(pos.x, pos.y, pos.z - 1);
            return _terrain.Contains(below) && !_terrain.Underground(below);
        }

        private static bool IsGroundDependentAt(BlockObject obj, Vector3Int pos)
        {
            var matter = obj.PositionedBlocks.GetBlock(pos).MatterBelow;
            return matter == MatterBelow.Ground || matter == MatterBelow.GroundOrStackable;
        }
    }

    // Justified Harmony boundary: these two lifecycle methods belong to Rainborn.
    // The private _blockObject is supplied through Harmony's named field injection.
    // No reflection is performed by our callback.
    [HarmonyPatch(typeof(RainbornExcavationFlag), "OnEnterFinishedState")]
    internal static class FinishedExcavationPatch
    {
        private static void Postfix(BlockObject ___blockObject)
        {
            var service = ExcavationCompatibilityService.Current;
            if (service == null) return;
            try { service.CleanAtFlag(___blockObject, "terrain removal"); }
            catch (Exception ex) { Debug.LogWarning("[TB-KS-AR] Post-excavation cleanup skipped: " + ex); }
        }
    }

    [HarmonyPatch(typeof(RainbornExcavationFlag), "OnDeleted")]
    internal static class LooseDirtPatch
    {
        private static void Prefix(BlockObject ___blockObject)
        {
            var service = ExcavationCompatibilityService.Current;
            if (service == null) return;
            try { service.CleanAtFlag(___blockObject, "Loose Dirt placement"); }
            catch (Exception ex) { Debug.LogWarning("[TB-KS-AR] Loose Dirt cleanup skipped: " + ex); }
        }
    }

    // Target the typed two-argument overload. A thread-local guard stops nested validation,
    // including if another mod also invokes the same game method.
    [HarmonyPatch(typeof(BlockValidator), nameof(BlockValidator.BlocksValid),
        new[] { typeof(BlockObjectSpec), typeof(Placement) })]
    internal static class RainbornPlacementPatch
    {
        [ThreadStatic] private static bool _checking;
        private static bool _warned;

        private static void Postfix(BlockValidator __instance, BlockObjectSpec __0,
            Placement __1, ref bool __result)
        {
            if (!__result || _checking) return;
            var service = ExcavationCompatibilityService.Current;
            if (service == null || !service.IsRainbornSpec(__0)) return;
            _checking = true;
            try { __result = service.ValidPlacement(__instance, __1); }
            catch (Exception ex)
            {
                __result = false; // Never allow unsafe terrain digging on a compatibility failure.
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning("[TB-KS-AR] Rainborn placement validation failed (further errors suppressed): " + ex);
                }
            }
            finally { _checking = false; }
        }
    }
}
