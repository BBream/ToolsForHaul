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
    public static class Toils_Cart
    {
        public static Toil MountOn(TargetIndex CartInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                cart.GetComp<CompMountable>().MountOn(actor);
            };
            return toil;
        }

        public static Toil DismountAt(TargetIndex CartInd, TargetIndex StoreCellInd)
        {
            Toil toil = new Toil();
            toil.initAction = () => 
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                cart.GetComp<CompMountable>().DismountAt(toil.actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
            };
            return toil;
        }

        public static Toil FindStoreCellForCart(TargetIndex CartInd)
        {
            const int NearbyCell = 8;
            const int RegionCellOffset = 16;
            IntVec3 invalid = new IntVec3(0, 0, 0);
            #if DEBUG
            StringBuilder stringBuilder = new StringBuilder();
            #endif
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                IntVec3 storeCell = IntVec3.Invalid;
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                //Find Valid Storage
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(cart.Position, NearbyCell, false))
                {
                    if (cell.IsValidStorageFor(cart) 
                        && ReservationUtility.CanReserveAndReach(actor, cell, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger(actor)))
                    {
                        storeCell = cell;
                        #if DEBUG
                        stringBuilder.AppendLine("Found cell: " + storeCell);
                        #endif
                    }
                }

                if (storeCell == IntVec3.Invalid)
                {
                    //Regionwise Flood-fill cellFinder
                    int regionInd = 0;
                    List<Region> regions = new List<Region>();
                    regions.Add(cart.Position.GetRegion());
                    #if DEBUG
                    stringBuilder.AppendLine(actor.LabelCap + " Report");
                    #endif
                    bool flag1 = false;
                    while (regionInd < regions.Count)
                    {
                        #if DEBUG
                        stringBuilder.AppendLine("Region id: " + regions[regionInd].id);
                        #endif
                        if (regions[regionInd].extentsClose.Center.InHorDistOf(cart.Position, NearbyCell + RegionCellOffset))
                        {
                            IntVec3 foundCell = IntVec3.Invalid;
                            float distFoundCell = float.MaxValue;
                            foreach (IntVec3 cell in regions[regionInd].Cells)
                            {
                                //Find best cell for placing cart
                                if (cell.GetEdifice() == null && cell.GetZone() == null && cell.Standable()
                                && !GenAdj.CellsAdjacentCardinal(cell, Rot4.North, IntVec2.One).Any(cardinal => cardinal.GetEdifice() is Building_Door)
                                && ReservationUtility.CanReserveAndReach(actor, cell, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger(actor)))
                                {
                                    if (regions[(regionInd > 0)? regionInd - 1: 0].extentsClose.Center.DistanceToSquared(cell) < distFoundCell)
                                    {
                                        foundCell = cell;
                                        distFoundCell = regions[(regionInd > 0) ? regionInd - 1 : 0].extentsClose.Center.DistanceToSquared(cell);
                                        flag1 = true;
                                    }
                                }
                            }
                            if (flag1 == true)
                            {
                                storeCell = foundCell;
                                #if DEBUG
                                stringBuilder.AppendLine("Found cell: " + storeCell);
                                #endif
                                break;
                            }
                            foreach (RegionLink link in regions[regionInd].links)
                            {
                                if (regions.Contains(link.RegionA) == false)
                                    regions.Add(link.RegionA);
                                if (regions.Contains(link.RegionB) == false)
                                    regions.Add(link.RegionB);
                            }
                        }
                        regionInd++;
                    }
                }
                //Log.Message(stringBuilder.ToString());
                /*
                //Home Area
                if (storeCell == IntVec3.Invalid)
                    foreach (IntVec3 cell in Find.AreaHome.ActiveCells.Where(cell => (cell.GetZone() == null || cell.IsValidStorageFor(cart)) && cell.Standable() && cell.GetEdifice() == null))
                        if (cell.DistanceToSquared(cart.Position) < NearbyCell)
                            storeCell = cell;
                */
                ReservationUtility.Reserve(actor, storeCell);
                toil.actor.jobs.curJob.targetB = (storeCell != invalid && storeCell != IntVec3.Invalid) ? storeCell : cart.Position;
            };
            return toil;
        }



        ///////////////
        //Animal Cart//
        ///////////////

        private const int defaultWaitWorker = 2056;
        private const int tickCheckInterval = 64;
        private static readonly JobDef jobDefStandby = DefDatabase<JobDef>.GetNamed("Standby");

        public static Toil CallAnimalCart(TargetIndex CartInd, TargetIndex Ind)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                Job job = new Job(jobDefStandby, toil.actor.jobs.curJob.GetTarget(Ind), defaultWaitWorker);
                cart.mountableComp.Driver.jobs.StartJob(job, JobCondition.InterruptForced);
            };
            return toil;
        }

        public static Toil ReleaseAnimalCart(TargetIndex CartInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                if (cart.mountableComp.IsMounted && cart.mountableComp.Driver.CurJob.def == jobDefStandby)
                    cart.mountableComp.Driver.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            };
            return toil;
        }

        public static Toil WaitAnimalCart(TargetIndex CartInd, TargetIndex HaulableInd)
        {
            Toil toil = new Toil();
            int tickTime = 0;
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                tickTime = 0;
                if (cart.mountableComp.IsMounted)
                {
                    //Worker is arrival and Animal cart is coming
                    if (cart.mountableComp.Driver.CurJob.def == jobDefStandby && !actor.Position.AdjacentTo8WayOrInside(cart))
                        tickTime = 0;
                    //Worker is arrival and Animal cart is arrival
                    else if (cart.mountableComp.Driver.CurJob.def == jobDefStandby && actor.Position.AdjacentTo8WayOrInside(cart))
                        toil.actor.jobs.curDriver.ReadyForNextToil();
                    //Worker is arrival but Animal cart is missing
                    else
                    {
                        Job job = new Job(jobDefStandby, actor.jobs.curJob.GetTarget(HaulableInd), defaultWaitWorker);
                        cart.mountableComp.Driver.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }
                else
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
            };
            toil.tickAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                if (Find.TickManager.TicksGame % tickCheckInterval == 0)
                    if (cart.mountableComp.IsMounted)
                    {
                        //Animal cart is arrival
                        if (cart.mountableComp.Driver.CurJob.def == jobDefStandby && actor.Position.AdjacentTo8WayOrInside(cart))
                            toil.actor.jobs.curDriver.ReadyForNextToil();
                        //Animal cart would never come. Imcompletable.
                        else if (cart.mountableComp.Driver.CurJob.def != jobDefStandby || tickTime >= defaultWaitWorker)
                            toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    }
                    else
                        toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

            
    }
}
