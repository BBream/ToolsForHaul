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
    //Toil Collect

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

    //Toil Drop

    public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode)
    {
        Toil toil = new Toil();
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            Job curJob = actor.jobs.curJob;
            if (actor.inventory.container.Count <= 0)
                return;
            Thing dropThing = actor.inventory.container.First();
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

            return;
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
                dropThing = actor.inventory.container.First();

            if (dropThing == null)
            {
                //Log.Error(toil.actor + "try drop null thing in " + actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
                return;
            }
            IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
            Thing dummy;

            Find.DesignationManager.RemoveAllDesignationsOn(dropThing);
            actor.inventory.container.TryDrop(dropThing, destLoc, placeMode, out dummy);
            //dropThing.holder = null;

            return;
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
            Thing dropThing = carrier.storage.First(); 
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
            return;
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
            return;
        };
        return toil;
    }
                
}}

