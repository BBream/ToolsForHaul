using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_DismountInBase : WorkGiver_Scanner
    {
        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);
        private static List<Thing> vehicleInt;
        private static int vehicleIntNum;

        public WorkGiver_DismountInBase() : base() { }

        private List<Thing> Cart()
        {
            int currentVehicleNum = Find.ListerThings.AllThings.Count((Thing vehicle) => (vehicle is Vehicle_Cart));
            if (vehicleInt == null || (vehicleInt != null && vehicleIntNum != currentVehicleNum))
            {
                vehicleIntNum = currentVehicleNum;
                vehicleInt = Find.ListerThings.AllThings.FindAll((Thing vehicle) => (vehicle is Vehicle_Cart));
            }
            return vehicleInt;
        }
        private static bool AvailableCart(Thing thing, Pawn pawn)
        {
            Vehicle_Cart vehicle = thing as Vehicle_Cart;
            if (vehicle == null)
                return false;
            return ((vehicle.TryGetComp<CompMountable>().IsMounted && vehicle.TryGetComp<CompMountable>().Driver == pawn)
                            || !vehicle.TryGetComp<CompMountable>().IsMounted);
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
            if (Cart().Any(vehicle => Find.Reservations.FirstReserverOf(vehicle, Faction.OfColony) == pawn && vehicle == t))
                return false;
            return (!t.IsForbidden(pawn.Faction) 
                && (pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Some))
                && (AvailableCart(t, pawn) || AvailableAnimalCart(t)));
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return Cart() as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return (ListerHaulables.ThingsPotentiallyNeedingHauling().Count <= 0                           //No Haulable
                    && !Cart().Any(vehicle => pawn.CanReserveAndReach(vehicle, PathEndMode.Touch, Danger.Some) && ((Vehicle_Cart)vehicle).storage.Count > 0)); //No cart need to drop                  
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            if (!(t is Vehicle_Cart))
                return null;
            Vehicle_Cart cart = t as Vehicle_Cart;
            Job jobDismountInBase = new Job(DefDatabase<JobDef>.GetNamed("DismountInBase"));

            //Set carrier
            jobDismountInBase.targetA = cart;
            ReservationUtility.Reserve(pawn, cart);

            //Move cart in Base
            jobDismountInBase.targetB = FindStorageCell(pawn, cart);

            return jobDismountInBase.targetB != IntVec3.Invalid ? jobDismountInBase : null;
        }


        private IntVec3 FindStorageCell(Pawn pawn, Thing closestHaulable)
        {
            foreach (var slotGroup in Find.SlotGroupManager.AllGroupsListInPriorityOrder)
            {
                foreach (var cell in slotGroup.CellsList.Where(cell =>
                            StoreUtility.IsValidStorageFor(cell, closestHaulable) && pawn.CanReserve(cell)))
                    if (cell != invalidCell && cell != IntVec3.Invalid)
                        return cell;
            }

            return IntVec3.Invalid;
        }

    }

}