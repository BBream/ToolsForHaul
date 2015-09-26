using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;         // Always neededJobGiver_Test
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
//using Verse.Sound;       // Needed when you do something with Sound
//using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
//using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace Vehicle
{
    public class JobGiver_Carrier : ThinkNode_JobGiver
    {
        bool flagInProgressDrop = false;
        public JobGiver_Carrier() { }

        public Job TryGetJobFor(Pawn pawn)
        {
            return TryGiveTerminalJob(pawn);
        }

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            Thing closestHaulable;
            Job jobCollectThing = new Job(DefDatabase<JobDef>.GetNamed("CollectThing"));
            Job jobDropInCell = new Job(DefDatabase<JobDef>.GetNamed("DropInCell"));
            Job jobDismountInBase = new Job(DefDatabase<JobDef>.GetNamed("DismountInBase"));
            jobCollectThing.maxNumToCarry = 99999;
            jobCollectThing.haulMode = HaulMode.ToCellStorage;

            
            //Find Available Carrier
            Vehicle_Cargo carrier = Find.ListerThings.AllThings.Find((Thing t)
                => (t.TryGetComp<CompMountable>() != null && t.TryGetComp<CompMountable>().Driver == pawn)) as Vehicle_Cargo;

            //No Carrier
            if (carrier == null)
            {
                //Log.Message("No Carrier");
                return null;
            }
            jobCollectThing.targetC = carrier;
            jobDropInCell.targetC = carrier;
            jobDismountInBase.targetA = carrier;

            //collectThing Predicate
            Predicate<Thing> predicate = (Thing t)
                => (!t.IsForbidden(pawn.Faction) && !t.IsInAnyStorage() &&
                pawn.CanReserve(t) && carrier.storage.CanAcceptAnyOf(t));

            //Log.Message("flagInProgressDrop" + flagInProgressDrop);
            if (carrier.storage.TotalStackCount < carrier.GetMaxStackCount && carrier.storage.Contents.Count() < carrier.maxItem
                && !ListerHaulables.ThingsPotentiallyNeedingHauling().NullOrEmpty() && flagInProgressDrop == false)
            {
                //Log.Message("Finding Haulable");
                closestHaulable = GenClosest.ClosestThing_Global_Reachable(pawn.Position,
                                                                ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                PathMode.ClosestTouch,
                                                                TraverseParms.For(pawn, Danger.Deadly, false),
                                                                9999,
                                                                predicate);
                if (closestHaulable == null)
                {
                    //Log.Message("No Haulable");
                    return null;
                }
                jobCollectThing.targetA = closestHaulable;
                return jobCollectThing;
            }
            else
            {
                //Log.Message("flagInProgressDrop" + flagInProgressDrop);
                flagInProgressDrop = true;
                if (carrier.storage.Contents.Count() <= 0)
                {
                    flagInProgressDrop = false;
                    //Log.Message("End Progress Drop");
                    if (ListerHaulables.ThingsPotentiallyNeedingHauling().NullOrEmpty())
                        return jobDismountInBase;
                    return null;
                }

                foreach (Zone zone in Find.ZoneManager.AllZones)
                {
                    if (zone is Zone_Stockpile)
                        foreach (var zoneCell in zone.cells)
                        {
                            Thing dropThing = carrier.storage.Contents.Last();

                            if (zoneCell.IsValidStorageFor(dropThing) && pawn.CanReserve(zoneCell))
                            {
                                jobDropInCell.targetA = dropThing;
                                jobDropInCell.targetB = zoneCell;
                                return jobDropInCell;
                            }
                        }
                }

                //No zone for stock
                //Log.Message("No Zone");
                return null;
            }
        }
    }
}
