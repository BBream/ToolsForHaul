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
        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);
        private static List<Thing> vehicleInt;
        private static int vehicleIntNum;

        public WorkGiver_HaulWithCart() : base() { }

        private List<Thing> Cart()
        {
            int currentVehicleNum = Find.ListerThings.AllThings.Count((Thing thing) => (thing is Vehicle_Cart));
            if (vehicleInt == null || (vehicleInt != null && vehicleIntNum != currentVehicleNum))
            {
                vehicleIntNum = currentVehicleNum;
                vehicleInt = Find.ListerThings.AllThings.FindAll((Thing thing) => (thing is Vehicle_Cart));
            }
            return vehicleInt;
        }
        private static bool AvailableCart(Thing thing, Pawn pawn)
        {
            Vehicle_Cart vehicle = thing as Vehicle_Cart;
            if (vehicle == null)
                return false;
            //Not mounted or Driver is pawnself
            return (!vehicle.TryGetComp<CompMountable>().IsMounted || vehicle.TryGetComp<CompMountable>().Driver == pawn);
        }
        private static bool AvailableAnimalCart(Thing thing)
        {
            Vehicle_Cart vehicle = thing as Vehicle_Cart;
            if (vehicle == null)
                return false;
            return vehicle.TryGetComp<CompMountable>().IsMounted && vehicle.TryGetComp<CompMountable>().Driver.RaceProps.Animal
                            && vehicle.TryGetComp<CompMountable>().Driver.needs.food.CurCategory < HungerCategory.Starving
                            && vehicle.TryGetComp<CompMountable>().Driver.needs.rest.CurCategory < RestCategory.VeryTired
                            && vehicle.TryGetComp<CompMountable>().Driver.health.State == PawnHealthState.Mobile
                            && vehicle.TryGetComp<CompMountable>().Driver.health.ShouldBeTreatedNow == false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            //Check that pawn already has reserved cart. 
            Thing reservedThing = Cart().Find(vehicle => Find.Reservations.FirstReserverOf(vehicle, Faction.OfColony) == pawn);
            if (reservedThing != null)
                return (reservedThing == t)? true : false;

            //Check Cart is valid for using.
            return (!t.IsForbidden(pawn.Faction) 
                && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Some)
                && (AvailableCart(t, pawn) || AvailableAnimalCart(t)));
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return Cart() as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            //DebugWriteHaulingPawn(pawn);
            return (Cart().Count == 0                                                                          //No Cart
                    || (ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0                           //Or No Haulable
                    && !Cart().Any(cart => pawn.CanReserve(cart) && ((Vehicle_Cart)cart).storage.Count > 0))); //   No cart need to drop                  
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;
            if (cart == null)
                return null;

            IEnumerable<Thing> remainingItems = cart.storage;
            int reservedMaxItem = cart.storage.Count;
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithCart"));
            if (cart.mountableComp.IsMounted && cart.mountableComp.Driver.RaceProps.Animal)
                jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart"));
            jobNew.targetQueueA = new List<TargetInfo>();
            jobNew.targetQueueB = new List<TargetInfo>();
            jobNew.targetC = cart;
            ReservationUtility.Reserve(pawn, cart);

            //Drop remaining item
            foreach (Thing remainingItem in remainingItems)
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
            while (reservedMaxItem < cart.MaxItem)
            {
                IntVec3 storageCell = IntVec3.Invalid;
                Thing closestHaulable = null;

                IntVec3 searchPos;
                searchPos = (!jobNew.targetQueueA.NullOrEmpty() && jobNew.targetQueueA.First().Thing.Position != IntVec3.Invalid) ?
                    jobNew.targetQueueA.First().Thing.Position : cart.Position;
                int maxDistance;
                maxDistance = (!jobNew.targetQueueA.NullOrEmpty() && jobNew.targetQueueA.First().Thing.Position != IntVec3.Invalid) ?
                    30 : 9999;

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
                reservedMaxItem++;
            }

            //Has job?
            if (!jobNew.targetQueueA.NullOrEmpty() && !jobNew.targetQueueB.NullOrEmpty())
                return jobNew;

            //No haulables or zone. Release everything
            Find.Reservations.ReleaseAllClaimedBy(pawn);
            return (Job)null;
        }


        private IntVec3 FindStorageCell(Pawn pawn, Thing closestHaulable, List<TargetInfo> targetQueue)
        {
            //Find closest cell in queue.
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

        private void DebugWriteHaulingPawn(Pawn pawn)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(pawn.LabelCap + " Report: Vehicle " + Cart().Count + " - ");
            foreach (Pawn other in Find.ListerPawns.FreeColonistsSpawned)
            {
                if (other.CurJob != null && other.CurJob.def == JobDefOf.HaulToCell)
                    stringBuilder.AppendLine(other.LabelCap + " Job: " + other.CurJob.def.defName);
            }
            foreach (Vehicle_Cart vehicle in Cart())
            {
                string driver = ((vehicle.mountableComp.IsMounted) ? vehicle.mountableComp.Driver.LabelCap : "No Driver");
                string state = "";
                if (vehicle.IsForbidden(pawn.Faction))
                    state = string.Concat(state, "Forbidden ");
                if (pawn.CanReserveAndReach(vehicle, PathEndMode.Touch, Danger.Some))
                    state = string.Concat(state, "CanReserveAndReach ");
                if (AvailableCart(vehicle, pawn))
                    state = string.Concat(state, "AvailableCart ");
                if (AvailableAnimalCart(vehicle))
                    state = string.Concat(state, "AvailableAnimalCart ");
                Pawn reserver = Find.Reservations.FirstReserverOf(vehicle, Faction.OfColony);
                if (reserver != null)
                    state = string.Concat(state, reserver.LabelCap, " Job: ", reserver.CurJob.def.defName);
                stringBuilder.AppendLine(vehicle.LabelCap + "- " + driver + ": " + state);

            }
            Log.Message(stringBuilder.ToString());
        }

    }

}