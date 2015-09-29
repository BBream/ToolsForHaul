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
    public class WorkGiver_HaulWithTools : WorkGiver_Scanner
    {
        private static List<Thing> availableVehicleInt;
        private static Pawn availableVehicleIntPawn;
        bool hasBackpack;

        private List<Thing> AvailableVehicle(Pawn pawn)
        {
            if (availableVehicleInt == null || (availableVehicleInt != null && availableVehicleIntPawn != pawn))
            {
                availableVehicleIntPawn = pawn;
                availableVehicleInt = Find.ListerThings.AllThings.FindAll((Thing vehicle)
                    => ((vehicle is Vehicle_Cart) && !vehicle.IsForbidden(pawn.Faction) && pawn.CanReserveAndReach(vehicle, PathEndMode.Touch, Danger.Some)
                    && ((!vehicle.TryGetComp<CompMountable>().IsMounted || vehicle.TryGetComp<CompMountable>().Driver == pawn)        //HaulWithCart
                    || (vehicle.TryGetComp<CompMountable>().IsMounted && vehicle.TryGetComp<CompMountable>().Driver.RaceProps.Animal  //HaulWithAnimalCart
                        && vehicle.TryGetComp<CompMountable>().Driver.needs.food.CurCategory != HungerCategory.Hungry
                        && vehicle.TryGetComp<CompMountable>().Driver.needs.rest.CurCategory != RestCategory.Tired))//Driver is animal not hungry and restless
                    ));
                return availableVehicleInt;
            }
            else
                return availableVehicleInt;
        }

        public WorkGiver_HaulWithTools() : base() 
        {
            hasBackpack = false;
        }
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
            //HaulWithBackpack
            foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack)
                {
                    hasBackpack = true;
                    return ListerHaulables.ThingsPotentiallyNeedingHauling();
                }

            //HaulWithCart
            hasBackpack = false;
            return AvailableVehicle(pawn) as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            int haulItemCount = ListerHaulables.ThingsPotentiallyNeedingHauling().Count;

            //HaulWithBackpack
            foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack && haulItemCount > 0)
                {
                    hasBackpack = true;
                    return false;
                }

            //HaulWithCart
            if (AvailableVehicle(pawn).Find(vehicle => ((Vehicle_Cart)vehicle).storage.Count > 0) != null || haulItemCount > 0)
            {
                hasBackpack = false;
                return false;
            }
            return true;

        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Job jobNew = null;
            Apparel_Backpack backpack = null;
            Vehicle_Cart carrier = null;
            //HaulWithBackpack
            if (hasBackpack)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                    if (apparel is Apparel_Backpack)
                    {
                        backpack = apparel as Apparel_Backpack;
                        break;
                    }

                if (backpack != null)
                {
                    jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithBackpack"));
                    jobNew.targetC = backpack;
                }
            }
            //HaulWithCart
            else
            {
                carrier = t as Vehicle_Cart;
                if (carrier != null)
                {
                    jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithCart"));
                    if (carrier.mountableComp.IsMounted && carrier.mountableComp.Driver.RaceProps.Animal)
                        jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart"));
                    jobNew.targetC = carrier;
                    ReservationUtility.Reserve(pawn, carrier);
                }
            }
            if (jobNew == null)
            {
                Log.Error("job is null. It should not be null");
                return (Job)null;
            }

            int reservedStorage = (hasBackpack) ? pawn.inventory.container.Count : carrier.storage.Count;
            int maxItem = (hasBackpack) ? backpack.maxItem : carrier.MaxItem;
            jobNew.targetQueueA = new List<TargetInfo>();
            jobNew.targetQueueB = new List<TargetInfo>();

            //HaulWithCart
            //Drop remaining item
            if (!hasBackpack)
            {
                IEnumerable<Thing> remainingItems = carrier.storage;
                foreach (var remainingItem in remainingItems)
                {
                    IntVec3 storageCell = FindStorageCell(pawn, remainingItem, jobNew.targetQueueB);
                    if (!storageCell.IsValid) break;

                    ReservationUtility.Reserve(pawn, storageCell);
                    jobNew.targetQueueB.Add(storageCell);
                }
                return jobNew;
            }

            //collectThing Predicate
            Predicate<Thing> predicate = item
                => !jobNew.targetQueueA.Contains(item) && pawn.CanReserve(item) && !item.IsInValidBestStorage();

            //Collect and drop item
            while (reservedStorage < maxItem)
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
                                                                            TraverseParms.For(pawn, Danger.Some),
                                                                            maxDistance,
                                                                            predicate);
                if (closestHaulable == null) break;

                storageCell = FindStorageCell(pawn, closestHaulable, jobNew.targetQueueB);
                if (storageCell == IntVec3.Invalid) break;
                jobNew.targetQueueA.Add(closestHaulable);
                jobNew.targetQueueB.Add(storageCell);
                ReservationUtility.Reserve(pawn, closestHaulable);
                ReservationUtility.Reserve(pawn, storageCell);
                reservedStorage++;
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
                    if (cell != new IntVec3(0, 0, 0) && cell != IntVec3.Invalid)
                        return cell;
            }

            return IntVec3.Invalid;
        }

    }

}