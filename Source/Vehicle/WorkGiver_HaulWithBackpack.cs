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
    public class WorkGiver_HaulWithBackpack : WorkGiver_Scanner
    {
        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);


        public WorkGiver_HaulWithBackpack() : base() { }
        /*
        public virtual PathEndMode PathEndMode { get; }
        public virtual ThingRequest PotentialWorkThingRequest { get; }

        public virtual bool HasJobOnCell(Pawn pawn, IntVec3 c);
        public virtual bool HasJobOnThing(Pawn pawn, Thing t);
        public virtual Job JobOnCell(Pawn pawn, IntVec3 cell);
        public virtual Job JobOnThing(Pawn pawn, Thing t);
        public PawnActivityDef MissingRequiredActivity(Pawn pawn);
        public virtual IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn);
        public virtual IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn Pawn);
        public virtual bool ShouldSkip(Pawn pawn);
         */
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {            
            /*foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack)
                    yield return apparel;*/
            //Need dummy for just running this workGiver
            return ListerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            //Don't have haulables.
            if (ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
                return true;

            //Should skip pawn that don't have backpack.
            foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack)
                    return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Apparel_Backpack backpack = null;
            foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack)
                    backpack = apparel as Apparel_Backpack;

            if (backpack == null)
                return (Job)null;


            int reservedMaxItem = pawn.inventory.container.Count;
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithBackpack"));
            //jobCollect.maxNumToCarry = 99999;
            //jobCollect.haulMode = HaulMode.ToCellStorage;
            jobNew.targetQueueA = new List<TargetInfo>();
            jobNew.targetQueueB = new List<TargetInfo>();
            jobNew.targetC = backpack;

            //collectThing Predicate
            Predicate<Thing> predicate = item
                => !jobNew.targetQueueA.Contains(item) && pawn.CanReserve(item) && !item.IsInValidBestStorage();

            //Collect and drop item
            while (reservedMaxItem < backpack.maxItem)
            {
                IntVec3 storageCell = IntVec3.Invalid;
                Thing closestHaulable = null;

                IntVec3 searchPos;
                searchPos = (!jobNew.targetQueueA.NullOrEmpty() && jobNew.targetQueueA.First() != IntVec3.Invalid) ?
                    jobNew.targetQueueA.First().Thing.Position : searchPos = pawn.Position;
                int maxDistance;
                maxDistance = (!jobNew.targetQueueA.NullOrEmpty() && jobNew.targetQueueA.First() != IntVec3.Invalid) ?
                    9999 : 8;

                closestHaulable = GenClosest.ClosestThing_Global_Reachable(searchPos,
                                                                            ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                            PathEndMode.Touch,
                                                                            TraverseParms.For(pawn),
                                                                            maxDistance,
                                                                            predicate);
                if (closestHaulable == null) break;

                storageCell = FindStorageCell(pawn, closestHaulable, jobNew.targetQueueB);
                if (storageCell == IntVec3.Invalid) break;
                jobNew.targetQueueA.Add(closestHaulable);
                jobNew.targetQueueB.Add(storageCell);
                ReservationUtility.Reserve(pawn, closestHaulable);
                ReservationUtility.Reserve(pawn, storageCell);
                reservedMaxItem++;
            }

            //Has job?
            if (!jobNew.targetQueueA.NullOrEmpty() && !jobNew.targetQueueB.NullOrEmpty())
                return jobNew;

            return (Job)null;
        }

        private IntVec3 FindStorageCell(Pawn pawn, Thing closestHaulable, List<TargetInfo> targetQueue)
        {
            if (!targetQueue.NullOrEmpty())
                foreach (TargetInfo target in targetQueue)
                    foreach (var adjCell in GenAdjFast.AdjacentCells8Way(target))
                        if (!targetQueue.Contains(adjCell) && adjCell.IsValidStorageFor(closestHaulable) && pawn.CanReserveAndReach(adjCell, PathEndMode.OnCell, Danger.Deadly))
                            return adjCell;

            foreach (var slotGroup in Find.SlotGroupManager.AllGroupsListInPriorityOrder)
            {
                foreach (var cell in slotGroup.CellsList.Where(cell =>
                            !targetQueue.Contains(cell) && StoreUtility.IsValidStorageFor(cell, closestHaulable) && pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly)))
                    if (cell != invalidCell && cell != IntVec3.Invalid)
                        return cell;
            }

            return IntVec3.Invalid;
        }

    }

}