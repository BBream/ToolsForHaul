using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;


namespace ToolsForHaul
{
    public class WorkGiver_HaulWithCart : WorkGiver_Scanner
    {
        private static List<Thing> availableVehicle;
        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);


        public WorkGiver_HaulWithCart() : base() {}
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
            availableVehicle = Find.ListerThings.AllThings.FindAll((Thing aV)
            => ((aV is Vehicle_Cart) && !aV.IsForbidden(pawn.Faction) && pawn.CanReserveAndReach(aV, PathEndMode.Touch, Danger.Some)
            && ((!aV.TryGetComp<CompMountable>().IsMounted || aV.TryGetComp<CompMountable>().Driver == pawn)        //HaulWithCart
            || (aV.TryGetComp<CompMountable>().IsMounted && aV.TryGetComp<CompMountable>().Driver.RaceProps.Animal  //HaulWithAnimalCart
                && aV.TryGetComp<CompMountable>().Driver.needs.food.CurCategory != HungerCategory.Hungry
                && aV.TryGetComp<CompMountable>().Driver.needs.rest.CurCategory != RestCategory.Tired))//Driver is animal not hungry and restless
            ));
            return availableVehicle as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            availableVehicle = PotentialWorkThingsGlobal(pawn) as List<Thing>;

            return (availableVehicle.Find(aV => ((Vehicle_Cart)aV).storage.TotalStackCount > 0) == null //Need to drop
                    && ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0);        //No Haulable
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Vehicle_Cart carrier = t as Vehicle_Cart;
            if (carrier == null)
                return null;

            IEnumerable<Thing> remainingItems = carrier.storage;
            int reservedMaxItem = carrier.storage.Count;
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithCart"));
            if (carrier.mountableComp.IsMounted && carrier.mountableComp.Driver.RaceProps.Animal)
                jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart"));
            //jobNew.maxNumToCarry = 99999;
            //jobNew.haulMode = HaulMode.ToCellStorage;
            jobNew.targetQueueA = new List<TargetInfo>();
            jobNew.targetQueueB = new List<TargetInfo>();

            //Set carrier
            jobNew.targetC = carrier;
            ReservationUtility.Reserve(pawn, carrier);

            //Drop remaining item
            foreach (var remainingItem in remainingItems)
            {
                IntVec3 storageCell = FindStorageCell(pawn, remainingItem, jobNew.targetQueueB);
                if (!storageCell.IsValid) break;
                
                ReservationUtility.Reserve(pawn, storageCell);
                jobNew.targetQueueB.Add(storageCell);
            }
            if (!jobNew.targetQueueB.NullOrEmpty())
                return jobNew;

            //collectThing Predicate
            Predicate<Thing> predicate = item
                => !jobNew.targetQueueA.Contains(item) && pawn.CanReserve(item) && !item.IsInValidBestStorage();

            //Collect and drop item
            while (reservedMaxItem < carrier.MaxItem)
            {
                IntVec3 storageCell = IntVec3.Invalid;
                Thing closestHaulable = null;

                IntVec3 searchPos;
                searchPos = (!jobNew.targetQueueA.NullOrEmpty() && jobNew.targetQueueA.First() != IntVec3.Invalid) ?
                    jobNew.targetQueueA.First().Thing.Position : searchPos = carrier.Position;

                closestHaulable = GenClosest.ClosestThing_Global_Reachable(searchPos,
                                                                            ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                            PathEndMode.Touch,
                                                                            TraverseParms.For(pawn, Danger.Some),
                                                                            9999,
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

            //No haulables or zone. Release everything
            Find.Reservations.ReleaseAllClaimedBy(pawn);
            return null;
        }


        private IntVec3 FindStorageCell(Pawn pawn, Thing closestHaulable, List<TargetInfo> targetQueue)
        {
            if (!targetQueue.NullOrEmpty())
                foreach (TargetInfo target in targetQueue)
                    foreach (var adjCell in GenAdjFast.AdjacentCells8Way(target))
                        if (!targetQueue.Contains(adjCell) && adjCell.IsValidStorageFor(closestHaulable) && pawn.CanReserve(adjCell))
                            return adjCell;

            foreach (var slotGroup in Find.SlotGroupManager.AllGroupsListInPriorityOrder)
            {
                foreach (var cell in slotGroup.CellsList.Where(cell =>
                            !targetQueue.Contains(cell) && StoreUtility.IsValidStorageFor(cell, closestHaulable) && pawn.CanReserve(cell)))
                    if (cell != invalidCell && cell != IntVec3.Invalid)
                        return cell;
            }

            return IntVec3.Invalid;
        }

    }

}