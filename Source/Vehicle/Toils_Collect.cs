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
    private const int NearbyCell = 30;

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

    #region Toil Collect
    public static Toil CheckDuplicates(Toil jumpToil, TargetIndex CarrierInd, TargetIndex HaulableInd)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            IntVec3 storeCell = IntVec3.Invalid;
            Pawn actor = toil.GetActor();

            TargetInfo target = toil.actor.jobs.curJob.GetTarget(HaulableInd);
            if (target.Thing.def.stackLimit <= 1) 
                return;
            List<TargetInfo> targetQueue = toil.actor.jobs.curJob.GetTargetQueue(HaulableInd);
            if (!targetQueue.NullOrEmpty() && target.Thing.def.defName == targetQueue.First().Thing.def.defName)
            {
                toil.actor.jobs.curJob.SetTarget(HaulableInd, targetQueue.First());
                Find.Reservations.Reserve(actor, targetQueue.First());
                targetQueue.RemoveAt(0);
                toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                return;
            }
            Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
            Apparel_Backpack backpack = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Apparel_Backpack;
            if (cart == null && backpack == null)
            {
                Log.Error(actor.LabelCap + " Report: Don't have Carrier");
                toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                return;
            }
            int curItemCount = (cart != null ? cart.storage.Count : actor.inventory.container.Count) + targetQueue.Count;
            int curItemStack = (cart != null ? cart.storage.TotalStackCount : actor.inventory.container.TotalStackCount) 
                                + targetQueue.Sum(item => item.Thing.stackCount);
            int maxItem = cart != null ? cart.MaxItem : backpack.MaxItem;
            int maxStack = cart != null ? cart.MaxStack : backpack.MaxStack;
            if (curItemCount >= maxItem || curItemStack >= maxStack)
                return;
            //Check target's nearby
            Thing thing = GenClosest.ClosestThing_Global_Reachable(actor.Position,
                                                                    ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                    PathEndMode.Touch,
                                                                    TraverseParms.For(actor, Danger.Some),
                                                                    NearbyCell,
                                                                    item => !targetQueue.Contains(item)
                                                                        && item.def.defName == target.Thing.def.defName
                                                                        && !FireUtility.IsBurning(item)
                                                                        && Find.Reservations.CanReserve(actor, item));
            if (thing != null)
            {
                toil.actor.jobs.curJob.SetTarget(HaulableInd, thing);
                Find.Reservations.Reserve(actor, thing);
                toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                return;
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
    #endregion

    #region Toil Drop
    public static Toil CheckNeedStorageCell(Toil jumpToil, TargetIndex CarrierInd, TargetIndex StoreCellInd)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;

            Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
            Apparel_Backpack backpack = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Apparel_Backpack;
            if (cart == null && backpack == null)
            {
                Log.Error(actor.LabelCap + " Report: Don't have Carrier");
                toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
            }
            ThingContainer container = cart != null ? cart.storage : actor.inventory.container;
            if (container.Count == 0)
                return;

            IntVec3 cell = ToolsForHaulUtility.FindStorageCell(actor, container.First());
            if (cell != IntVec3.Invalid)
            {
                toil.actor.jobs.curJob.SetTarget(StoreCellInd, cell);
                Find.Reservations.Reserve(actor, cell);
                toil.actor.jobs.curDriver.JumpToToil(jumpToil);
            }
        };
        return toil;
    }

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

            if (destLoc.GetStorable() == null)
            {
                Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
                actor.inventory.container.TryDrop(dropThing, destLoc, placeMode, out dummy);
            }

            //Check cell queue is adjacent
            List<TargetInfo> cells = curJob.GetTargetQueue(StoreCellInd);
            for (int i = 0; i < cells.Count && i < actor.inventory.container.Count; i++)
                if (destLoc.AdjacentTo8Way(cells[i].Cell) && cells[i].Cell.GetStorable() == null)
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(actor.inventory.container[i]);
                    actor.inventory.container.TryDrop(actor.inventory.container[i], cells[i].Cell, ThingPlaceMode.Direct, out dummy);
                    cells.RemoveAt(i);
                    i--;
                }
            //Check item queue is valid storage for adjacent cell
            foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc))
                if (actor.inventory.container.Count > 0 && adjCell.GetStorable() == null && StoreUtility.IsValidStorageFor(adjCell, actor.inventory.container.First()))
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(actor.inventory.container.First());
                    actor.inventory.container.TryDrop(actor.inventory.container.First(), adjCell, ThingPlaceMode.Direct, out dummy);
                }
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
                for (int i = 0; i + 1 < actor.inventory.container.Count; i++)
                    if (actor.inventory.container[i] == lastItem)
                        dropThing = actor.inventory.container[i + 1];
            else if (lastItem == null && actor.inventory.container.Count > 0)
                dropThing = actor.inventory.container.First();

            if (dropThing == null)
            {
                //Log.Error(toil.actor + "try drop null thing in " + actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
                return;
            }
            IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
            Thing dummy;

            if (destLoc.GetStorable() == null)
            {
                Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
                actor.inventory.container.TryDrop(dropThing, destLoc, placeMode, out dummy);
            }
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

            if (destLoc.GetStorable() == null)
            {
                Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
                carrier.storage.TryDrop(dropThing, destLoc, placeMode, out dummy);
            }

            //Check cell queue is adjacent
            List<TargetInfo> cells = curJob.GetTargetQueue(StoreCellInd);
            for (int i = 0; i < cells.Count && i < carrier.storage.Count; i++)
                if (destLoc.AdjacentTo8Way(cells[i].Cell) && cells[i].Cell.GetStorable() == null)
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(carrier.storage[i]);
                    carrier.storage.TryDrop(carrier.storage[i], cells[i].Cell, ThingPlaceMode.Direct, out dummy);
                    cells.RemoveAt(i);
                    i--;
                }
            //Check item queue is valid storage for adjacent cell
            foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc))
                if (carrier.storage.Count > 0 && adjCell.GetStorable() == null && StoreUtility.IsValidStorageFor(adjCell, carrier.storage.First()))
                {
                    Find.DesignationManager.RemoveAllDesignationsOn(carrier.storage.First());
                    carrier.storage.TryDrop(carrier.storage.First(), adjCell, ThingPlaceMode.Direct, out dummy);
                }
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
    #endregion
}}

