using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace IsoRimVFEFactoryPackZ
{
    // IsoRim Waylith - VFE Factory Pack, IsoRimZ part (loaded only with IsoRimZ: LoadFolders.xml 1.7/IsoRimZ).
    //  - Autofarmer (user 2026-10-29: allowed upstairs): its zone is kept to its own floor. VFE walks rows in front of the
    //    machine up to 30 cells (Building_Autofarmer.GetRowCells); on a floored map the rows north of a band are the void
    //    rows and the next floor, so a ground autofarmer near the band's edge harvested and sowed crops upstairs. Every
    //    zone cell (harvest, sow, obstruction check, the zone overlay, the hoppers' rows) comes from GetRowCells, so its
    //    cells are filtered there to those on the machine's floor (ProjectZ.Api.SameFloor).
    //  - Drill platform ground-only: XML (1.7/IsoRimZ/Defs/GroundOnly.xml, ProjectZ.GroundOnlyDef).
    // VFE Factory is reached by name: a missing type or method leaves it as VFE has it (logged once).
    [StaticConstructorOnStartup]
    public static class PackZ
    {
        internal const string Tag = "[IsoRim VFE Factory Pack Z] ";

        static PackZ()
        {
            try
            {
                Type farmer = AccessTools.TypeByName("VanillaFurnitureExpandedFactory.Building_Autofarmer");
                MethodInfo rows = farmer != null ? AccessTools.Method(farmer, "GetRowCells", new[] { typeof(int) }) : null;
                if (rows == null)
                {
                    Log.Warning(Tag + "VFE Factory's Building_Autofarmer.GetRowCells was not found: autofarmer zones are not kept to their floor.");
                    return;
                }
                new Harmony("peren.isorimwaylith.vfefactorypack.z").Patch(rows, postfix: new HarmonyMethod(typeof(PackZ), nameof(Rows_Postfix)));
            }
            catch (Exception e)
            {
                Log.Error(Tag + "Setup failed (autofarmer zones are not kept to their floor): " + e);
            }
        }

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
    }
}
