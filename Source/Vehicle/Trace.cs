#define DEBUG

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace ToolsForHaul
{
    public static class Trace
    {
        public static StringBuilder stringBuilder = new StringBuilder();
        public static Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        [Conditional("DEBUG")]
        public static void AppendLine(String str)
        {
            stringBuilder.AppendLine((stopWatch.IsRunning ? stopWatch.ElapsedMilliseconds + "ms: " : "") + str);
        }
        [Conditional("DEBUG")]
        public static void LogMessage()
        {
            Log.Message(stringBuilder.ToString());
            stringBuilder.Remove(0, stringBuilder.Length);
            stopWatch.Reset();
        }
        [Conditional("DEBUG")]
        public static void DebugWriteHaulingPawn(Pawn pawn)
        {
            Trace.AppendLine(pawn.LabelCap + " Report: Cart " + ToolsForHaulUtility.Cart().Count + " Job: " + ((pawn.CurJob != null) ? pawn.CurJob.def.defName : "No Job")
                + " Backpack: " + ((ToolsForHaulUtility.TryGetBackpack(pawn) != null) ? "True" : "False")
                + " lastGivenWorkType: " + pawn.mindState.lastGivenWorkType);
            foreach (Pawn other in Find.ListerPawns.FreeColonistsSpawned)
            {
                //Vanilla haul or Haul with backpack
                if (other.CurJob != null && (other.CurJob.def == JobDefOf.HaulToCell || other.CurJob.def == DefDatabase<JobDef>.GetNamed("HaulWithBackpack")))
                    Trace.AppendLine(other.LabelCap + " Job: " + other.CurJob.def.defName
                        + " Backpack: " + ((ToolsForHaulUtility.TryGetBackpack(other) != null) ? "True" : "False")
                        + " lastGivenWorkType: " + other.mindState.lastGivenWorkType);
            }
            foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart())
            {
                string driver = ((cart.mountableComp.IsMounted) ? cart.mountableComp.Driver.LabelCap : "No Driver");
                string state = "";
                if (cart.IsForbidden(pawn.Faction))
                    state = string.Concat(state, "Forbidden ");
                if (pawn.CanReserveAndReach(cart, PathEndMode.Touch, Danger.Some))
                    state = string.Concat(state, "CanReserveAndReach ");
                if (ToolsForHaulUtility.AvailableCart(cart, pawn))
                    state = string.Concat(state, "AvailableCart ");
                if (ToolsForHaulUtility.AvailableAnimalCart(cart))
                    state = string.Concat(state, "AvailableAnimalCart ");
                Pawn reserver = Find.Reservations.FirstReserverOf(cart, Faction.OfColony);
                if (reserver != null)
                    state = string.Concat(state, reserver.LabelCap, " Job: ", reserver.CurJob.def.defName);
                Trace.AppendLine(cart.LabelCap + "- " + driver + ": " + state);

            }
            Trace.LogMessage();
        }
    }
}
