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
        private List<Thing> availableVehicle;
        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);

        public WorkGiver_DismountInBase() : base() { }
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
            => ((aV is Vehicle_Cart) && !aV.IsForbidden(pawn.Faction) && !aV.IsInValidBestStorage()
            && ((!aV.TryGetComp<CompMountable>().IsMounted && pawn.CanReserve(aV))   //Unmounted
                || aV.TryGetComp<CompMountable>().Driver == pawn)                  //or Driver is pawnself
            ));

            #if DEBUG
            Log.Message("Number of Reservation:" + Find.Reservations.AllReservedThings().Count().ToString());
            //Log.Message("availableVehicle Count: " + availableVehicle.Count);
            #endif
            return availableVehicle as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return Find.ListerThings.AllThings.Find((Thing aV)
            => ((aV is Vehicle_Cart) && !aV.IsForbidden(pawn.Faction) && !aV.IsInValidBestStorage()
            && ((!aV.TryGetComp<CompMountable>().IsMounted && pawn.CanReserve(aV))   //Unmounted
                || aV.TryGetComp<CompMountable>().Driver == pawn)                  //or Driver is pawnself
            )) == null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            if (!(t is Vehicle_Cart))
                return null;
            Vehicle_Cart carrier = t as Vehicle_Cart;
            Job jobDismountInBase = new Job(DefDatabase<JobDef>.GetNamed("DismountInBase"));

            //Set carrier
            jobDismountInBase.targetA = carrier;
            ReservationUtility.Reserve(pawn, carrier);

            //Move cart in Base
            jobDismountInBase.targetB = FindStorageCell(pawn, carrier);

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