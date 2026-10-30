using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IsoRimVFEFactoryPackZ
{
    // IsoRim Waylith - VFE Factory Pack, IsoRimZ part (loaded only with IsoRimZ: LoadFolders.xml 1.7/IsoRimZ).
    //  - Autofarmer (user 2026-10-29: allowed upstairs): its zone is kept to its own floor. VFE walks rows in front of the
    //    machine up to 30 cells (Building_Autofarmer.GetRowCells); on a floored map the rows north of a band are the void
    //    rows and the next floor, so a ground autofarmer near the band's edge harvested and sowed crops upstairs. Every
    //    zone cell (harvest, sow, obstruction check, the zone overlay, the hoppers' rows) comes from GetRowCells, so its
    //    cells are filtered there to those on the machine's floor (ProjectZ.Api.SameFloor).
    //  - Autofarmer and floors over it (batch B5): its 3D gantry (2.16 tall, drawn by the IsoRim part) runs over the
    //    footprint and the zone, but IsoRimZ's height rule sees only the base model (0.20). So:
    //      * the autofarmer counts as too tall for a floor over it (ProjectZ.Api.RegisterHeightRule): no slab over its
    //        footprint, and it can't be placed under one (VFE's PlaceWorker_NotUnderRoof refuses that too);
    //      * no slab over its zone (the rows of its current zone length): GenConstruct.CanPlaceBlueprintAt is refused for a
    //        slab (a def with ProjectZ's PlaceWorker_UpperFloor) whose cell below is in an autofarmer's zone;
    //      * no autofarmer placed where its first zone (VFE's starting 3 rows) has a floor over it;
    //      * a zone that has a floor over it anyway (the zone made longer under a slab) counts as obstructed
    //        (Building_Autofarmer.IsZoneObstructed): VFE says so and the gantry does not start.
    //  - Drill platform ground-only: XML (1.7/IsoRimZ/Defs/GroundOnly.xml, ProjectZ.GroundOnlyDef).
    // VFE Factory is reached by name: a missing type or method leaves it as VFE has it (logged once).
    [StaticConstructorOnStartup]
    public static class PackZ
    {
        internal const string Tag = "[IsoRim VFE Factory Pack Z] ";
        private const string FarmerDef = "VFEFactory_Autofarmer";
        private const int FirstZone = 3;   // VFE's starting zone length (Building_Autofarmer.zoneLength)

        private static ThingDef farmer;
        private static PropertyInfo cellsOf;

        static PackZ()
        {
            Harmony h = new Harmony("peren.isorimwaylith.vfefactorypack.z");
            try
            {
                Type type = AccessTools.TypeByName("VanillaFurnitureExpandedFactory.Building_Autofarmer");
                MethodInfo rows = type != null ? AccessTools.Method(type, "GetRowCells", new[] { typeof(int) }) : null;
                if (rows == null)
                {
                    Log.Warning(Tag + "VFE Factory's Building_Autofarmer.GetRowCells was not found: autofarmer zones are not kept to their floor.");
                    return;
                }
                h.Patch(rows, postfix: new HarmonyMethod(typeof(PackZ), nameof(Rows_Postfix)));
                Floors(h, type);
            }
            catch (Exception e)
            {
                Log.Error(Tag + "Setup failed (autofarmer zones are not kept to their floor): " + e);
            }
        }

        // ---------------------------------------------------------------- zone on its own floor
        public static void Rows_Postfix(Thing __instance, ref IEnumerable<IntVec3> __result)
        {
            Map map = __instance?.Map;
            if (map == null || __result == null || !ProjectZ.Api.HasFloors(map))
            {
                return;
            }
            __result = OnFloor(__result, __instance.Position, map);
        }

        private static IEnumerable<IntVec3> OnFloor(IEnumerable<IntVec3> cells, IntVec3 home, Map map)
        {
            foreach (IntVec3 c in cells)
            {
                if (ProjectZ.Api.SameFloor(c, home, map))
                {
                    yield return c;
                }
            }
        }

        // ---------------------------------------------------------------- no floor over the gantry's path
        private static void Floors(Harmony h, Type type)
        {
            farmer = DefDatabase<ThingDef>.GetNamedSilentFail(FarmerDef);
            cellsOf = AccessTools.Property(type, "Cells");
            MethodInfo obstructed = AccessTools.Method(type, "IsZoneObstructed");
            if (farmer == null || cellsOf == null || obstructed == null || ProjectZ.Api.Version < 3)
            {
                Log.Warning(Tag + "VFE Factory's autofarmer (Cells, IsZoneObstructed) or ProjectZ Api 3 not found: floors over autofarmers are not refused.");
                return;
            }
            RegisterRule();
            h.Patch(obstructed, postfix: new HarmonyMethod(typeof(PackZ), nameof(Obstructed_Postfix)));
            h.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintAt_NewTemp)),
                postfix: new HarmonyMethod(typeof(PackZ), nameof(CanPlace_Postfix)));
        }

        private static void RegisterRule()
        {
            ProjectZ.Api.RegisterHeightRule(def => def == farmer ? true : (bool?)null);
        }

        // A slab (ProjectZ's upper floor, built or planned) right over a ground cell c of its floor.
        private static bool FloorOver(IntVec3 c, Map map)
        {
            IntVec3 up = ProjectZ.Api.StackedCell(c, map, true);
            if (!up.IsValid)
            {
                return false;
            }
            TerrainDef t = up.GetTerrain(map);
            if (t != null && t.defName != "PZ_OpenAir")
            {
                return true;
            }
            List<Thing> list = map.thingGrid.ThingsListAtFast(up);
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i] is Blueprint || list[i] is Frame) && IsSlab(list[i].def.entityDefToBuild))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSlab(BuildableDef def)
        {
            List<Type> pw = def?.placeWorkers;
            if (pw == null)
            {
                return false;
            }
            for (int i = 0; i < pw.Count; i++)
            {
                if (pw[i] != null && pw[i].Name == "PlaceWorker_UpperFloor")
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<IntVec3> ZoneOf(Thing f)
        {
            return cellsOf.GetValue(f) as IEnumerable<IntVec3> ?? Array.Empty<IntVec3>();
        }

        public static void Obstructed_Postfix(Thing __instance, ref bool __result)
        {
            try
            {
                Map map = __instance?.Map;
                if (__result || map == null || !ProjectZ.Api.HasFloors(map))
                {
                    return;
                }
                foreach (IntVec3 c in ZoneOf(__instance))
                {
                    if (FloorOver(c, map))
                    {
                        __result = true;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce(Tag + "Checking an autofarmer's zone for floors failed: " + e, 93690011);
            }
        }

        public static void CanPlace_Postfix(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, ref AcceptanceReport __result)
        {
            if (!__result.Accepted || map == null || entDef == null || !ProjectZ.Api.HasFloors(map))
            {
                return;
            }
            try
            {
                if (entDef == farmer)
                {
                    foreach (IntVec3 c in FirstZoneCells(center, rot))
                    {
                        if (c.InBounds(map) && ProjectZ.Api.SameFloor(c, center, map) && FloorOver(c, map))
                        {
                            __result = "VFEF_IsoRimZ_FloorOverField".Translate();
                            return;
                        }
                    }
                }
                else if (IsSlab(entDef))
                {
                    IntVec3 below = ProjectZ.Api.StackedCell(center, map, false);
                    if (!below.IsValid)
                    {
                        return;
                    }
                    foreach (Thing f in map.listerThings.ThingsOfDef(farmer))
                    {
                        if ((f.Position - below).LengthHorizontalSquared > 35 * 35)
                        {
                            continue;   // zones reach at most 30 rows
                        }
                        foreach (IntVec3 c in ZoneOf(f))
                        {
                            if (c == below)
                            {
                                __result = "VFEF_IsoRimZ_OverField".Translate();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce(Tag + "Checking floors against autofarmers failed: " + e, 93690012);
            }
        }

        // VFE's rows 1..FirstZone in front of an autofarmer placed at center facing rot (Building_Autofarmer.GetRowCells).
        private static IEnumerable<IntVec3> FirstZoneCells(IntVec3 center, Rot4 rot)
        {
            CellRect r = GenAdj.OccupiedRect(center, rot, farmer.size);
            IntVec3 right = rot.RighthandCell;
            IntVec3 edge;
            int half;
            if (rot == Rot4.North) { edge = new IntVec3(r.CenterCell.x, 0, r.maxZ); half = (r.Width - 1) / 2; }
            else if (rot == Rot4.South) { edge = new IntVec3(r.CenterCell.x, 0, r.minZ); half = (r.Width - 1) / 2; }
            else if (rot == Rot4.East) { edge = new IntVec3(r.maxX, 0, r.CenterCell.z); half = (r.Height - 1) / 2; }
            else { edge = new IntVec3(r.minX, 0, r.CenterCell.z); half = (r.Height - 1) / 2; }
            for (int i = 1; i <= FirstZone; i++)
            {
                for (int j = -half; j <= half; j++)
                {
                    yield return edge + rot.FacingCell * i + right * j;
                }
            }
        }
    }
}
