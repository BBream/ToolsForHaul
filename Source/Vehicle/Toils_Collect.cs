using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;



namespace ToolsForHaul{
public static class Toils_Collect
{
    private const int NearbyCell = 5;

    public static Toil Extract(TargetIndex ind)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            List<TargetInfo> targetQueue = toil.actor.jobs.curJob.GetTargetQueue(ind);
            if (!targetQueue.NullOrEmpty())
            {
                toil.actor.jobs.curJob.SetTarget(ind, targetQueue.First());
                targetQueue.RemoveAt(0);
            }
        };
        return toil;
    }

    public static Toil CheckDuplicates(Toil jumpToil, TargetIndex CarrierInd, TargetIndex HaulableInd)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            IntVec3 storeCell = IntVec3.Invalid;
            Pawn actor = toil.GetActor();
            Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
            Apparel_Backpack backpack = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Apparel_Backpack;
            TargetInfo target = toil.actor.jobs.curJob.GetTarget(HaulableInd);
            List<TargetInfo> targetQueue = toil.actor.jobs.curJob.GetTargetQueue(HaulableInd);
            if (cart == null && backpack == null)
            {
                Log.Error(actor.LabelCap + " Report: Don't have Carrier");
                toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
            }
            Thing thing = GenClosest.ClosestThing_Global_Reachable(actor.Position,
                                                                    ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                    PathEndMode.Touch,
                                                                    TraverseParms.For(actor, Danger.Some),
                                                                    NearbyCell,
                                                                    t => t.def.defName == target.Thing.def.defName && !targetQueue.Contains(t));

            if (thing != null && Find.Reservations.CanReserve(actor, thing))
            {
                if ((cart != null && cart.storage.Count + targetQueue.Count < cart.MaxItem
                    && cart.storage.TotalStackCount + targetQueue.Sum(t => t.Thing.stackCount) < cart.GetMaxStackCount)
                    ||
                    (backpack != null && actor.inventory.container.Count + targetQueue.Count < backpack.maxItem
                    && actor.inventory.container.TotalStackCount + targetQueue.Sum(t => t.Thing.stackCount) < backpack.maxItem * 100)
                   )
                {
                    targetQueue.Add(thing);
                    Find.Reservations.Reserve(actor, thing);
                    toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                }
            }
        };
        return toil;
    }

    public static Toil CollectInInventory(TargetIndex HaulableInd)
	{

		Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

            //Check haulThing is human_corpse. If other race has apparel, It need to change
            if ((haulThing.ThingID.IndexOf("Human_Corpse") <= -1)? false : true)
            {
                Corpse corpse = (Corpse)haulThing;
                var wornApparel = corpse.innerPawn.apparel.WornApparel;

                //Drop wornApparel. wornApparel cannot Add to container directly because it will be duplicated.
                corpse.innerPawn.apparel.DropAll(corpse.innerPawn.Position, false);

                //Transfer in container
                foreach (Thing apparel in wornApparel)
                {
                    if (actor.inventory.container.TryAdd(apparel))
                    {
                        apparel.holder = actor.inventory.GetContainer();
                        apparel.holder.owner = actor.inventory;
                    }
                }
            }
            //Collecting TargetIndex ind
            if (actor.inventory.container.TryAdd(haulThing))
            {
                haulThing.holder = actor.inventory.GetContainer();
                haulThing.holder.owner = actor.inventory;
            }

        };
        toil.FailOn(() =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

            if (!actor.inventory.container.CanAcceptAnyOf(haulThing))
                return true;

            return false;
        });
		return toil;
	}

    public static Toil CollectInCarrier(TargetIndex CarrierInd, TargetIndex HaulableInd)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            Thing haulThing = curJob.GetTarget(HaulableInd).Thing;
            Vehicle_Cart carrier = curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
            //Check haulThing is human_corpse. If other race has apparel, It need to change

            Find.DesignationManager.RemoveAllDesignationsOn(haulThing);
            if ((haulThing.ThingID.IndexOf("Human_Corpse") <= -1) ? false : true)
            {
                Corpse corpse = (Corpse)haulThing;
                var wornApparel = corpse.innerPawn.apparel.WornApparel;

                //Drop wornApparel. wornApparel cannot Add to container directly because it will be duplicated.
                corpse.innerPawn.apparel.DropAll(corpse.innerPawn.Position, false);

                //Transfer in container
                foreach (Thing apparel in wornApparel)
                {
                    if (carrier.storage.TryAdd(apparel))
                    {
                        apparel.holder = carrier.GetContainer();
                        apparel.holder.owner = carrier;
                    }
                }
            }
            //Collecting TargetIndex ind
            if (carrier.storage.TryAdd(haulThing))
            {
                haulThing.holder = carrier.GetContainer();
                haulThing.holder.owner = carrier;
            }

            List<TargetInfo> thingList = curJob.GetTargetQueue(HaulableInd);
            for (int i = 0; i < thingList.Count; i++)
                if (actor.Position.AdjacentTo8Way(thingList[i].Thing.Position))
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(thingList[i].Thing);
                    if (carrier.storage.TryAdd(thingList[i].Thing))
                    {
                        thingList[i].Thing.holder = carrier.GetContainer();
                        thingList[i].Thing.holder.owner = carrier;
                    }
                    thingList.RemoveAt(i);
                    i--;
                }

        };
        toil.FailOn(() =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            Thing haulThing = curJob.GetTarget(HaulableInd).Thing;
            Vehicle_Cart carrier = curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;

            if (!carrier.storage.CanAcceptAnyOf(haulThing)
                && actor.Position.IsAdjacentTo8WayOrInside(haulThing.Position, haulThing.Rotation, haulThing.RotatedSize))
                return true;
            return false;
        });
        toil.FailOnDespawned(CarrierInd);
        return toil;
    }

    /////////////
    //Toil Drop//
    /////////////

    public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            if (actor.inventory.container.Count <= 0)
                return;
            toil.actor.jobs.curJob.SetTarget(TargetIndex.A, actor.inventory.container.First());
            Thing dropThing = toil.actor.jobs.curJob.targetA.Thing;
            IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
            Thing dummy;

            Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
            actor.inventory.container.TryDrop(dropThing, destLoc, placeMode, out dummy);

            //Check cell queue is adjacent
            List<TargetInfo> cellList = curJob.GetTargetQueue(StoreCellInd);
            for (int i = 0; i < cellList.Count && i < actor.inventory.container.Count; i++)
                if (destLoc.AdjacentTo8Way(cellList[i].Cell))
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(actor.inventory.container[i]);
                    actor.inventory.container.TryDrop(actor.inventory.container[i], cellList[i].Cell, ThingPlaceMode.Direct, out dummy);
                    cellList.RemoveAt(i);
                    i--;
                }
            //Check item queue is valid storage for adjacent cell
            foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc))
                if (actor.inventory.container.Count > 0 && StoreUtility.IsValidStorageFor(adjCell, actor.inventory.container.First()))
                    actor.inventory.container.TryDrop(actor.inventory.container.First(), adjCell, ThingPlaceMode.Direct, out dummy);
        };
        return toil;
    }

    public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode, Thing lastItem)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            if (actor.inventory.container.Count <= 0)
                return;

            //Check dropThing is last item that should not be dropped
            Thing dropThing = null;
            if (lastItem != null)
            {
                for (int i = 0; i + 1 < actor.inventory.container.Count; i++)
                    if (actor.inventory.container[i] == lastItem)
                        dropThing = actor.inventory.container[i + 1];
            }
            else if (lastItem == null && actor.inventory.container.Count >= 1)
            {
                toil.actor.jobs.curJob.SetTarget(TargetIndex.A, actor.inventory.container.First());
                dropThing = toil.actor.jobs.curJob.targetA.Thing;
            }

            if (dropThing == null)
            {
                //Log.Error(toil.actor + "try drop null thing in " + actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
                return;
            }
            IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
            Thing dummy;

            Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
            actor.inventory.container.TryDrop(dropThing, destLoc, placeMode, out dummy);
        };
        return toil;
    }

    public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode, TargetIndex CarrierInd)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            Vehicle_Cart carrier = actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
            if (carrier.storage.Count <= 0)
                return;
            toil.actor.jobs.curJob.SetTarget(TargetIndex.A, carrier.storage.First());
            Thing dropThing = toil.actor.jobs.curJob.targetA.Thing;
            IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
            Thing dummy;

            Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
            carrier.storage.TryDrop(dropThing, destLoc, placeMode, out dummy);

            //Check cell queue is adjacent
            List<TargetInfo> cellList = curJob.GetTargetQueue(StoreCellInd);
            for (int i = 0; i < cellList.Count && i < carrier.storage.Count; i++)
                if (destLoc.AdjacentTo8Way(cellList[i].Cell))
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(carrier.storage[i]);
                    carrier.storage.TryDrop(carrier.storage[i], cellList[i].Cell, ThingPlaceMode.Direct, out dummy);
                    cellList.RemoveAt(i);
                    i--;
                }
            //Check item queue is valid storage for adjacent cell
            foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc))
                if (carrier.storage.Count > 0 && StoreUtility.IsValidStorageFor(adjCell, carrier.storage.First()))
                    carrier.storage.TryDrop(carrier.storage.First(), adjCell, ThingPlaceMode.Direct, out dummy);
        };
        toil.FailOnDespawned(CarrierInd);
        return toil;
    }

    public static Toil DropAllInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;

            actor.inventory.container.TryDropAll(destLoc, placeMode);
        };
        return toil;
    }
                
}}

