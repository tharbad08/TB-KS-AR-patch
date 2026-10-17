using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Timberborn.ModManagerScene;

namespace TBKSARPatch
{
    // No references to MoM, Rainborn or Keystone assemblies: the target mods remain unmodified.
    public sealed class ModStarter : IModStarter
    {
        public void StartMod(IModEnvironment environment)
        {
            var harmony = new Harmony("tharbad08.TBKSARPatch");
            try
            {
                var flag = AccessTools.TypeByName("AcademicsRainborn.RainbornExcavationFlag");
                var validator = AccessTools.TypeByName("Timberborn.BlockSystem.BlockValidator");
                var blockService = AccessTools.TypeByName("Timberborn.BlockSystem.BlockService");
                var entityService = AccessTools.TypeByName("Timberborn.EntitySystem.EntityService");
                var terrainService = AccessTools.TypeByName("Timberborn.TerrainSystem.TerrainService");
                var templateMapper = AccessTools.TypeByName("Timberborn.TemplateSystem.TemplateNameMapper");
                var onFinish = AccessTools.Method(flag, "OnEnterFinishedState");
                var onDelete = AccessTools.Method(flag, "OnDeleted");
                var blocksValid = validator == null ? null : validator.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SingleOrDefault(m => m.Name == "BlocksValid" && m.GetParameters().Length == 2 && m.ReturnType == typeof(bool));
                if (onFinish == null || onDelete == null || blocksValid == null || blockService == null || entityService == null || terrainService == null || templateMapper == null)
                {
                    Log.Warn("Timberborn/Rainborn API mismatch: patch not activated (no partial patching).");
                    return;
                }
                // Capture the game services in the scene where they are created, not from a previous save.
                Capture(harmony, blockService, nameof(ServiceCapture.BlockService));
                Capture(harmony, entityService, nameof(ServiceCapture.EntityService));
                Capture(harmony, terrainService, nameof(ServiceCapture.TerrainService));
                Capture(harmony, templateMapper, nameof(ServiceCapture.TemplateMapper));
                harmony.Patch(onFinish, postfix: new HarmonyMethod(typeof(ExcavationPatches), nameof(ExcavationPatches.AfterTerrainRemoval)));
                harmony.Patch(onDelete, prefix: new HarmonyMethod(typeof(ExcavationPatches), nameof(ExcavationPatches.BeforeLooseDirt)));
                harmony.Patch(blocksValid, postfix: new HarmonyMethod(typeof(ExcavationPatches), nameof(ExcavationPatches.ValidateRainbornPlacement)));
                Log.Info("Active: vanilla dynamite-compatible excavation constraints and safe natural-resource cleanup.");
            }
            catch (Exception e) { Log.Warn("Patch activation failed: " + e); }
        }
        private static void Capture(Harmony harmony, Type type, string target)
        {
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(ServiceCapture), target));
        }
    }

    internal static class ServiceCapture
    {
        internal static object Blocks;
        internal static object Entities;
        internal static object Terrain;
        internal static object Mapper;
        internal static void BlockService(object __instance) { Blocks = __instance; }
        internal static void EntityService(object __instance) { Entities = __instance; }
        internal static void TerrainService(object __instance) { Terrain = __instance; }
        internal static void TemplateMapper(object __instance) { Mapper = __instance; }
    }

    internal static class Log
    {
        private static MethodInfo _info, _warn;
        private static void Write(string msg, bool warning)
        {
            try
            {
                var method = warning ? _warn : _info;
                if (method == null)
                {
                    var debug = AccessTools.TypeByName("UnityEngine.Debug");
                    method = debug?.GetMethod(warning ? "LogWarning" : "Log", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(object) }, null);
                    if (warning) _warn = method; else _info = method;
                }
                method?.Invoke(null, new object[] { "[TB-KS-AR] " + msg });
            }
            catch { /* Logging must never break the simulation. */ }
        }
        internal static void Info(string s) { Write(s, false); }
        internal static void Warn(string s) { Write(s, true); }
    }

    internal static class ExcavationPatches
    {
        private static bool _warnedMissingServices, _warnedDynamite, _warnedPlacement;
        private static int _removed;
        // Called AFTER Rainborn's native one-block UnsetTerrain. This method must not re-run UnsetTerrain.
        internal static void AfterTerrainRemoval(object __instance)
        {
            try { CleanAtFlag(__instance, "terrain removal"); }
            catch (Exception ex) { Log.Warn("Post-excavation cleanup skipped: " + ex); }
        }
        // Rainborn's OnDeleted checks BlocksValid before constructing Loose Dirt; remove
        // freshly spawned flora/rocks before that check, but never delete player buildings.
        internal static void BeforeLooseDirt(object __instance)
        {
            try { CleanAtFlag(__instance, "Loose Dirt placement"); }
            catch (Exception ex) { Log.Warn("Loose Dirt pre-check cleanup skipped: " + ex); }
        }

        internal static void ValidateRainbornPlacement(object __0, object __1, ref bool __result, object __instance)
        {
            if (!__result) return;
            try
            {
                if (!R.IsRainbornFlagTemplate(__1)) return;
                // Match the actual vanilla Dynamite template's block-occupancy validator;
                // then additionally enforce its one-layer digging constraints.
                var place = __0;
                var pos = R.PositionOfPlacement(place);
                if (pos == null || !R.CanExcavateTerrain(pos))
                {
                    __result = false;
                    return;
                }
                var dyn = R.GetDynamiteTemplate();
                if (dyn != null)
                {
                    var method = __instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .SingleOrDefault(m => m.Name == "BlocksValid" && m.GetParameters().Length == 2 &&
                            m.GetParameters()[1].ParameterType.IsInstanceOfType(dyn));
                    if (method != null)
                    {
                        // Re-enters this postfix for vanilla Dynamite, which isn't a Rainborn flag.
                        __result = (bool)method.Invoke(__instance, new[] { place, dyn });
                    }
                    else if (!_warnedPlacement)
                    {
                        _warnedPlacement = true;
                        Log.Warn("Vanilla Dynamite occupancy validator could not be resolved; using terrain check only.");
                    }
                }
                else if (!_warnedDynamite)
                {
                    _warnedDynamite = true;
                    Log.Warn("Vanilla Dynamite template not found; using terrain check only.");
                }
            }
            catch (Exception ex)
            {
                // Fail CLOSED for mod-specific placement if our extra validation fails;
                // do not permit digging into terrain with unknown restrictions.
                __result = false;
                Log.Warn("Rainborn flag placement check failed safely: " + ex);
            }
        }

        private static void CleanAtFlag(object flag, string stage)
        {
            var block = R.Member(flag, "_blockObject");
            var pos = R.Member(block, "Coordinates");
            if (pos == null) return;
            if (ServiceCapture.Blocks == null || ServiceCapture.Entities == null || ServiceCapture.Terrain == null)
            {
                if (!_warnedMissingServices) { _warnedMissingServices = true; Log.Warn("Game services unavailable; skipped one excavation cleanup."); }
                return;
            }
            // Rainborn destroys only ONE terrain voxel. Do not clean if ground remains.
            if (!R.IsTerrainAbsentBelow(pos)) return;
            int removedNow = 0;
            // Both the flag/Loose Dirt cell and the cell immediately above can contain
            // natural resources with their foot on the removed terrain. Check exact support.
            foreach (var coord in new[] { pos, R.ShiftZ(pos, 1) })
            {
                if (coord == null) continue;
                foreach (var obj in R.ObjectsAt(coord))
                {
                    if (obj == null || ReferenceEquals(obj, block)) continue;
                    if (!R.IsNaturalDecoration(obj) || !R.IsGroundDependentAt(obj, coord)) continue;
                    if (!R.IsTerrainAbsentBelow(coord)) continue;
                    if (R.DeleteEntity(obj)) removedNow++;
                }
            }
            if (removedNow > 0)
            {
                _removed += removedNow;
                if (_removed <= 10 || _removed % 50 == 0)
                    Log.Info("Removed " + removedNow + " unsupported natural decoration(s) at " + stage + ". Total " + _removed);
            }
        }
    }

    // Reflection only where a third-party API boundary requires it; no guessed game method signatures
    // and no runtime dependency on MoM. All queried public member names below were checked
    // against the user's installed Timberborn 1.1 and Rainborn 0.2.0.4 binaries.
    internal static class R
    {
        private static Type _rainSpec, _natural, _keystone, _templateSpec;
        private static Type Type(string fullname) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullname, false)).FirstOrDefault(t => t != null);
        internal static object Member(object obj, string name)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            for (var curr = t; curr != null; curr = curr.BaseType)
            {
                var f = curr.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(obj);
                var p = curr.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (p != null) return p.GetValue(obj, null);
            }
            return null;
        }
        private static object Call(object target, string name, params object[] args)
        {
            if (target == null) return null;
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var method = methods.FirstOrDefault(m => m.Name == name && !m.ContainsGenericParameters &&
                m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
            return method?.Invoke(target, args);
        }
        private static object Component(object bo, Type type)
        {
            if (bo == null || type == null) return null;
            var m = bo.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(q => q.Name == "GetComponent" && q.IsGenericMethodDefinition && q.GetParameters().Length == 0);
            return m?.MakeGenericMethod(type).Invoke(bo, null);
        }
        private static object Spec(object template, Type type)
        {
            if (template == null || type == null) return null;
            var m = template.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(q => q.Name == "GetSpec" && q.IsGenericMethodDefinition && q.GetParameters().Length == 0);
            return m?.MakeGenericMethod(type).Invoke(template, null);
        }
        internal static bool IsRainbornFlagTemplate(object template)
        {
            if (template == null) return false;
            if (_rainSpec == null) _rainSpec = Type("AcademicsRainborn.RainbornExcavationFlagSpec");
            try { if (Spec(template, _rainSpec) != null) return true; } catch { }
            // Fallback on the game-provided TemplateSpec rather than a guessed ToString.
            if (_templateSpec == null) _templateSpec = Type("Timberborn.TemplateSystem.TemplateSpec");
            try
            {
                var name = Member(Spec(template, _templateSpec), "TemplateName") as string;
                return name != null && name.IndexOf("RainbornExcavationFlag", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }
        internal static object GetDynamiteTemplate()
        {
            foreach (var name in new[] { "Dynamite", "Dynamite.Folktails", "Dynamite.IronTeeth" })
            {
                try { var template = Call(ServiceCapture.Mapper, "GetTemplate", name); if (template != null) return template; }
                catch { /* Alternative template name may be absent for a faction. */ }
            }
            return null;
        }
        internal static object PositionOfPlacement(object placement)
        {
            return Member(placement, "Coordinates") ?? Member(placement, "Position");
        }
        private static int? Coordinate(object obj, string axis)
        {
            var val = Member(obj, axis); return val == null ? (int?)null : Convert.ToInt32(val);
        }
        internal static object ShiftZ(object coords, int delta)
        {
            if (coords == null) return null;
            var x = Coordinate(coords, "x"); var y = Coordinate(coords, "y"); var z = Coordinate(coords, "z");
            if (!x.HasValue || !y.HasValue || !z.HasValue) return null;
            return Activator.CreateInstance(coords.GetType(), x.Value, y.Value, z.Value + delta);
        }
        internal static bool IsTerrainAbsentBelow(object cell)
        {
            var below = ShiftZ(cell, -1);
            if (below == null) return false;
            try { return !(bool)Call(ServiceCapture.Terrain, "Underground", below); }
            catch { return false; }
        }
        internal static bool CanExcavateTerrain(object flagCell)
        {
            var z = Coordinate(flagCell, "z");
            if (!z.HasValue || z.Value <= 0) return false; // vanilla dynamite: not on lowest terrain level
            var below = ShiftZ(flagCell, -1);
            if (below == null || ServiceCapture.Terrain == null) return false;
            try { return (bool)Call(ServiceCapture.Terrain, "Underground", below); }
            catch { return false; }
        }
        internal static object[] ObjectsAt(object pos)
        {
            try { return (Call(ServiceCapture.Blocks, "GetObjectsAt", pos) as IEnumerable)?.Cast<object>().ToArray() ?? Array.Empty<object>(); }
            catch { return Array.Empty<object>(); }
        }
        internal static bool IsNaturalDecoration(object bo)
        {
            if (_natural == null) _natural = Type("Timberborn.NaturalResources.NaturalResource");
            if (_keystone == null) _keystone = Type("Keystone.Mod.Recipes.KeystoneVariant");
            try { return Component(bo, _natural) != null || Component(bo, _keystone) != null; }
            catch { return false; }
        }
        internal static bool IsGroundDependentAt(object bo, object coord)
        {
            try
            {
                var positioned = Member(bo, "PositionedBlocks");
                var block = Call(positioned, "GetBlock", coord);
                var below = Member(block, "MatterBelow");
                var name = below?.ToString();
                return name == "Ground" || name == "GroundOrStackable";
            }
            catch { return false; }
        }
        internal static bool DeleteEntity(object obj)
        {
            try
            {
                var service = ServiceCapture.Entities;
                var method = service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "Delete" && m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType.IsAssignableFrom(obj.GetType()));
                if (method == null) return false;
                method.Invoke(service, new[] { obj });
                return true;
            }
            catch (Exception ex) { Log.Warn("Decoration cleanup failed safely: " + ex.Message); return false; }
        }
    }
}
