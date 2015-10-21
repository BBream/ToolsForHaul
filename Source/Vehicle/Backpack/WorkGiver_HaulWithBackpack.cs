//#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace ToolsForHaul
{
    public class WorkGiver_HaulWithBackpack : WorkGiver
    {
        private static string NoBackpack = Translator.Translate("NoBackpack");

        public override bool ShouldSkip(Pawn pawn)
        {
            Trace.DebugWriteHaulingPawn(pawn);
            //Don't have haulables.
            if (ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
                return true;

            //Should skip pawn that don't have backpack.
            if (ToolsForHaulUtility.TryGetBackpack(pawn) == null)
                    return true;
            return false;
        }

        public override Job NonScanJob(Pawn pawn)
        {
            if (ToolsForHaulUtility.TryGetBackpack(pawn) != null)
                return ToolsForHaulUtility.HaulWithTools(pawn);
            JobFailReason.Is(NoBackpack);
            return (Job)null;
        }
    }
}