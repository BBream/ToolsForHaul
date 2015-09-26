using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicle
{
    public class WorkGiver_DriverWorker : WorkGiver
    {
        public List<Thing> availableVehicle;

        public WorkGiver_DriverWorker() : base(){;}
        /*
        public virtual PathMode PathMode { get; }
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
            /*mountedVehicle = Find.ListerThings.AllThings.Find((Thing aV)
            => (aV.TryGetComp<CompMountable>() != null && aV.TryGetComp<CompMountable>().Driver == pawn) && pawn.CanReserve(aV, ReservationType.Total)) as Vehicle_Cargo;
            if (mountedVehicle != null)
            {
                //Log.Message("mountedVehicle owner: " + pawn.Name);
                return mountedVehicle as IEnumerable<Thing>;
            }*/

            
            availableVehicle = Find.ListerThings.AllThings.FindAll((Thing aV)
            => ( (aV is Vehicle_Cargo)&& !aV.IsForbidden(pawn.Faction) 
            && ((!aV.TryGetComp<CompMountable>().IsMounted && pawn.CanReserve(aV))   //Unmounted
                ||  aV.TryGetComp<CompMountable>().Driver == pawn)                  //or Driver is pawnself
            ));

            //Log.Message("availableVehicle Count: " + availableVehicle.Count);
            return availableVehicle as IEnumerable<Thing>;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            bool hasZone = false;
            foreach (Zone zone in Find.ZoneManager.AllZones)
                if (zone is Zone_Stockpile)
                    hasZone = true;

            if (availableVehicle != null || !hasZone)
                return true;

            //Log.Message("No availableVehicle");
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            if (!(t is Vehicle_Cargo))
                return null;
            Vehicle_Cargo carrier = t as Vehicle_Cargo;
            List<Thing> haulables = ListerHaulables.ThingsPotentiallyNeedingHauling();
            IEnumerable<Thing> remainingItems = carrier.storage.Contents;
            //bool IsMountedVehicle = ((carrier.GetComp<CompMountable>().Driver == pawn)? true: false);
            int reservedStackCount = carrier.storage.TotalStackCount;
            int reservedMaxItem = carrier.storage.Contents.Count();
            Job jobDismountInBase = new Job(DefDatabase<JobDef>.GetNamed("DismountInBase"));
            Job jobCollect = new Job(DefDatabase<JobDef>.GetNamed("Collect"));
            jobCollect.maxNumToCarry = 99999;
            jobCollect.haulMode = HaulMode.ToCellStorage;
            jobCollect.targetQueueA = new List<TargetInfo>();
            jobCollect.targetQueueB = new List<TargetInfo>();

            //Set carrier
            jobDismountInBase.targetA = carrier;
            jobCollect.targetC = carrier;
            ReservationUtility.Reserve(pawn, t);

            //If IgnoreForbidden is true, add forbidden
            if (carrier.ignoreForbidden)
            {
                List<Thing> forbiddens = Find.ListerThings.AllThings.FindAll((Thing thing) =>
                    (thing.TryGetComp<CompForbiddable>() != null && thing.TryGetComp<CompForbiddable>().Forbidden == true));
                haulables = forbiddens.Concat(haulables).ToList();
                /*string logStr = "";
                foreach (var forbidden in forbiddens)
                    logStr += forbidden.ThingID;
                Log.Message("Haulable: " + logStr);*/
            }

            //Drop remaining item
            foreach (var remainingItem in remainingItems)
            {
                IntVec3 storageCell = new IntVec3(-1000, -1000, -1000);
                foreach (Zone zone in Find.ZoneManager.AllZones)
                {
                    if (storageCell.IsValid) break;
                    if (zone is Zone_Stockpile)
                        foreach (var zoneCell in zone.cells)
                            if (!jobCollect.targetQueueB.Contains(zoneCell) && zoneCell.IsValidStorageFor(remainingItem) && pawn.CanReserve(zoneCell))
                            {
                                storageCell = zoneCell;
                                break;
                            }
                }
                if (!storageCell.IsValid) break;

                //Just drop, not collect. It was already collected
                //jobCollect.targetQueueA.Add(remainingItem);
                //ReservationUtility.Reserve(pawn, remainingItem, ReservationType.Total);
                jobCollect.targetQueueB.Add(storageCell);
                ReservationUtility.Reserve(pawn, storageCell);
            }
            if (!jobCollect.targetQueueB.NullOrEmpty())
                return jobCollect;

            //collectThing Predicate
            Predicate<Thing> predicate = (Thing item)
                => ((carrier.ignoreForbidden || !item.IsForbidden(pawn.Faction)) 
                && !item.IsInValidStorage() && pawn.CanReserve(item) 
                && carrier.storage.CanAcceptAnyOf(item));

            //Collect and drop item
            while (!haulables.NullOrEmpty() && reservedStackCount < carrier.GetMaxStackCount && reservedMaxItem < carrier.maxItem)
            {
                IntVec3 storageCell = new IntVec3(-1000, -1000, -1000);
                Thing closestHaulable = null;
                //Log.Message("reservedStackCount, reservedMaxItem :" + reservedStackCount + ',' +  reservedMaxItem);
                closestHaulable = GenClosest.ClosestThing_Global_Reachable(pawn.Position,
                                                                haulables,
                                                                PathMode.ClosestTouch,
                                                                TraverseParms.For(pawn, Danger.Deadly, false),
                                                                9999,
                                                                predicate);
                if (closestHaulable == null)
                    break;

                if (!jobCollect.targetQueueB.NullOrEmpty())
                    foreach (TargetInfo target in jobCollect.targetQueueB)
                    {
                        if (storageCell.IsValid) break;
                        foreach (var adjCell in GenAdjFast.AdjacentCells8Way(target))
                            if (!jobCollect.targetQueueB.Contains(adjCell) && adjCell.IsValidStorageFor(closestHaulable) && pawn.CanReserve(adjCell))
                            {
                                storageCell = adjCell;
                                break;
                            }
                    }

                foreach (Zone zone in Find.ZoneManager.AllZones)
                {
                    if (storageCell.IsValid) break;
                    if (zone is Zone_Stockpile)
                        foreach (var zoneCell in zone.cells)
                            if (!jobCollect.targetQueueB.Contains(zoneCell) && zoneCell.IsValidStorageFor(closestHaulable) && pawn.CanReserve(zoneCell))
                            {
                                storageCell = zoneCell;
                                //Log.Message("storageCell: " + storageCell);
                                break;
                            }
                }
                //No Storage
               if (!storageCell.IsValid)
                   break;

               jobCollect.targetQueueA.Add(closestHaulable);
               ReservationUtility.Reserve(pawn, closestHaulable);
               haulables.Remove(closestHaulable);
               jobCollect.targetQueueB.Add(storageCell);
               ReservationUtility.Reserve(pawn, storageCell);
               reservedMaxItem++;
               reservedStackCount += closestHaulable.stackCount;
            }

            //No haulables or zone
            if (jobCollect.targetQueueA.NullOrEmpty() || jobCollect.targetQueueB.NullOrEmpty())
            {
                foreach (Zone zone in Find.ZoneManager.AllZones)
                {
                    if (jobDismountInBase.targetB != null) break;
                    if (zone is Zone_Stockpile)
                        foreach (var zoneCell in zone.cells)
                        {
                            Thing dropThing = carrier;
                            if (zoneCell.IsValidStorageFor(dropThing) && pawn.CanReserve(zoneCell))
                            {
                                ReservationUtility.Reserve(pawn, zoneCell);
                                jobDismountInBase.targetB = zoneCell;
                                break;
                            }
                        }
                }

                //Move cargo in Base
                if (!carrier.IsInValidStorage() && jobDismountInBase.targetB != null)
                    return jobDismountInBase;

                //No job, no move to cargo
                if (Find.Reservations.IsReserved(carrier, pawn.Faction))
                    Find.Reservations.Release(carrier, pawn);
                return null;
            }

            return jobCollect;
        }
    }

}