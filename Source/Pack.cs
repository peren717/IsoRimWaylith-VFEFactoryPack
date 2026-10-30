using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;

namespace IsoRimVFEFactoryPack
{
    // IsoRim Waylith - VFE Factory Pack, IsoRim part (needs IsoRim Api 39; older: this part does nothing, logged once, and the
    // conveyor models are not added: Patches/ gates them on Api 39).
    //  - Conveyor models (Patches/VFEF_Models_*.xml): VFE picks each belt piece's sprite at runtime (Building_Conveyor.Graphic:
    //    straight, turn, merger, splitter, filter, refuelling port); IsoRim's byGraphic (Api 39) maps each sprite to a model,
    //    rebuilds it when the sprite changes and leaves out the flat sprite VFE draws every frame for some pieces. Items on the
    //    belts ride at the belt top: every belt model's surfaceHeight is BeltTop (IsoRim lifts what the belt draws to it).
    //  - What VFE draws itself with UnityEngine.Graphics (IsoRim does not see those meshes) is put right in 3D here:
    //    the flow arrows of a selected belt lie on the belt top instead of under the models (DrawFlowArrow); the
    //    underground ends' "no link" icon stands over the hood like IsoRim's other overlay icons (DrawAt); the stack-count
    //    labels of items on a belt sit under the item as seen by the 3D camera (GetLabelScreenPos).
    //  - Factory glow (batch B3): IsoRim draws glow* faces lit always (it darkens them only for a CompGlower, which VFE's
    //    factories have not). A VFE Factory machine without power is in the model state "vfef_unpowered"; ModelDefs with
    //    that state in byState (smelter, conveyor oven) draw their model with the glow faces dark then.
    // VFE Factory is reached by name: a missing type or method leaves that part as VFE draws it (logged once).
    [StaticConstructorOnStartup]
    public static class Pack
    {
        internal const string Tag = "[IsoRim VFE Factory Pack] ";

        // The shared item and port height (cells over the floor): every belt piece's top, the hopper's bin floor and spout,
        // and (factory batches) the bottom of every machine opening. Model notes: model-review\vfefactory_b1\analysis.md.
        public const float BeltTop = 0.22f;

        static Pack()
        {
            try
            {
                if (IsoRim.Api.Version < 39)
                {
                    Log.Message(Tag + "IsoRim is older than Api 39: VFE Factory conveyors keep IsoRim's automatic shapes.");
                    return;
                }
                Conveyors.Run(new Harmony("peren.isorimwaylith.vfefactorypack"));
                IsoRim.Api.AddModelState(Factories.State);
            }
            catch (Exception e)
            {
                Log.Error(Tag + "Setup failed (VFE Factory's belt overlays stay as VFE draws them in 3D): " + e);
            }
        }
    }

    internal static class Conveyors
    {
        private const string Ns = "VanillaFurnitureExpandedFactory.";
        private const float ArrowLift = 0.012f;    // flow arrows just over the belt top (the belt's own arrows are 0.007)
        private const float IconHeight = 0.75f;   // the "no link" icon over an underground end's hood (0.40)

        // The thing whose drawing is under way (set by the prefixes, read by the mesh helpers).
        [ThreadStatic] private static Thing current;

        private static readonly MethodInfo DrawMeshMatrix = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
            new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
        private static readonly MethodInfo DrawMeshPosRot = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
            new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) });

        internal static void Run(Harmony h)
        {
            Type conveyor = AccessTools.TypeByName(Ns + "Building_Conveyor");
            Type underground = AccessTools.TypeByName(Ns + "Building_UndergroundConveyorBase");
            if (conveyor == null)
            {
                Log.Warning(Pack.Tag + "VFE Factory's Building_Conveyor was not found: belt overlays stay as VFE draws them.");
                return;
            }
            TryPatch(h, AccessTools.Method(conveyor, "DrawFlowArrow"), "flow arrows",
                prefix: nameof(SetCurrent), transpiler: nameof(FlowArrow_Transpiler));
            TryPatch(h, AccessTools.Method(conveyor, "GetLabelScreenPos"), "stack labels", prefix: nameof(LabelPos_Prefix));
            if (underground != null)
            {
                TryPatch(h, AccessTools.DeclaredMethod(underground, "DrawAt"), "no-link icon",
                    prefix: nameof(SetCurrent), transpiler: nameof(NoLink_Transpiler));
            }
        }

        private static void TryPatch(Harmony h, MethodInfo m, string what, string prefix = null, string transpiler = null)
        {
            if (m == null)
            {
                Log.Warning(Pack.Tag + "VFE Factory method for the " + what + " not found: left as VFE draws it.");
                return;
            }
            try
            {
                h.Patch(m, prefix: prefix != null ? new HarmonyMethod(typeof(Conveyors), prefix) : null,
                    transpiler: transpiler != null ? new HarmonyMethod(typeof(Conveyors), transpiler) : null);
            }
            catch (Exception e)
            {
                Log.Warning(Pack.Tag + "Could not patch the " + what + " (left as VFE draws it): " + e.Message);
            }
        }

        public static void SetCurrent(Thing __instance)
        {
            current = __instance;
        }

        private static bool Modelled(Thing t)
        {
            return t != null && IsoRim.Api.ViewReady && IsoRim.Api.ModelStandsIn(t);
        }

        // Replaces the one Graphics.DrawMesh call of the method with the helper of the same arguments.
        private static IEnumerable<CodeInstruction> Swap(IEnumerable<CodeInstruction> code, MethodInfo from, string to)
        {
            MethodInfo helper = AccessTools.Method(typeof(Conveyors), to);
            int n = 0;
            foreach (CodeInstruction ci in code)
            {
                if (from != null && ci.Calls(from))
                {
                    ci.opcode = OpCodes.Call;
                    ci.operand = helper;
                    n++;
                }
                yield return ci;
            }
            if (n == 0)
            {
                Log.Warning(Pack.Tag + "VFE Factory's drawing changed (" + to + "): left as VFE draws it.");
            }
        }

        public static IEnumerable<CodeInstruction> FlowArrow_Transpiler(IEnumerable<CodeInstruction> code) => Swap(code, DrawMeshMatrix, nameof(FlowArrowMesh));

        public static IEnumerable<CodeInstruction> NoLink_Transpiler(IEnumerable<CodeInstruction> code) => Swap(code, DrawMeshPosRot, nameof(NoLinkMesh));

        // A selected belt's flow arrow (Building_Conveyor.DrawFlowArrow): VFE lays it flat on the ground at the item layer,
        // under the 3D belt; here it lies just over the belt top, on the belt's own floor.
        public static void FlowArrowMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer)
        {
            try
            {
                if (Modelled(current))
                {
                    matrix.m13 = (Pack.BeltTop + ArrowLift) / IsoRim.Api.HeightSquash;
                    matrix = IsoRim.Api.DrawnMatrix(matrix);
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce(Pack.Tag + "Placing a flow arrow failed: " + e, 93690001);
            }
            Graphics.DrawMesh(mesh, matrix, material, layer);
        }

        // The underground ends' "no link" icon (Building_UndergroundConveyorBase.DrawAt): stood up over the hood, as IsoRim
        // stands up the game's overlay icons, instead of lying flat on the ground inside the model.
        public static void NoLinkMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer)
        {
            if (Modelled(current))
            {
                try
                {
                    using (IsoRim.Api.Upright(IconHeight))
                    {
                        IsoRim.Api.DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer);
                    }
                    return;
                }
                catch (Exception e)
                {
                    Log.ErrorOnce(Pack.Tag + "Drawing the no-link icon failed: " + e, 93690002);
                }
            }
            Graphics.DrawMesh(mesh, position, rotation, material, layer);
        }

        // The stack count (or quality) label of the item on a belt (Building_Conveyor.GetLabelScreenPos): VFE projects the
        // flat map point 0.4 cells south of the item, which in 3D is off (camera turned, item lifted, upper floors). Here it
        // goes under the item at the belt top as the 3D camera sees it.
        public static bool LabelPos_Prefix(Thing __instance, Vector3 worldPos, ref Vector2 __result)
        {
            if (!IsoRim.Api.ViewReady)
            {
                return true;
            }
            try
            {
                Camera cam = Find.Camera;
                float h = IsoRim.Api.ModelStandsIn(__instance) ? Pack.BeltTop : 0f;
                Vector3 at = IsoRim.Api.DrawnPosition(new Vector3(worldPos.x, h, worldPos.z));
                Vector3 s = cam.WorldToScreenPoint(at);
                Vector3 r = cam.WorldToScreenPoint(at + cam.transform.right * 0.4f);
                float below = (r - s).magnitude;   // 0.4 cells on screen, as VFE's own offset
                float scale = Prefs.UIScale;
                __result = new Vector2(s.x / scale, (UI.screenHeight - s.y + below) / scale);
                return false;
            }
            catch (Exception e)
            {
                Log.ErrorOnce(Pack.Tag + "Placing a belt item label failed: " + e, 93690003);
                return true;
            }
        }
    }

    internal static class Factories
    {
        internal const string Unpowered = "vfef_unpowered";

        // A VFE Factory machine whose power is off (no grid, not enough power, switched off: CompPowerTrader.PowerOn).
        // Things without a power comp (belts, hoppers) and other mods' things have no state here.
        public static string State(Thing t)
        {
            if (t == null || !t.Spawned || !t.def.defName.StartsWith("VFEFactory_", StringComparison.Ordinal))
            {
                return null;
            }
            CompPowerTrader p = (t as ThingWithComps)?.GetComp<CompPowerTrader>();
            return p != null && !p.PowerOn ? Unpowered : null;
        }
    }
}
