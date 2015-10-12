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
                if (!Find.AreaHome.ActiveCells.Contains(cart.Position) || cart.Position.GetZone() != null)
                    foreach (IntVec3 cell in Find.AreaHome.ActiveCells.Where(cell => cell.GetZone() == null && cell.Standable()))
                    {
                        if ((cart.Position - cell).LengthHorizontalSquared < (cart.Position - storeCell).LengthHorizontalSquared)
                            storeCell = cell;
                    }
                toil.actor.jobs.curJob.targetB = (storeCell != IntVec3.Invalid)? storeCell : cart.Position;
            };
            return toil;
        }



        ///////////////
        //Animal Cart//
        ///////////////

        private const int defaultWaitWorker = 2400;
        private const int tickCheckInterval = 60;
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
                tickTime++;
                if (tickTime % tickCheckInterval == 0)
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
