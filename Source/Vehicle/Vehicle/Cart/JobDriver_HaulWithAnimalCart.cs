using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;


namespace ToolsForHaul
{
    public class JobDriver_HaulWithAnimalCart : JobDriver
    {
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;
        private const TargetIndex CarrierInd = TargetIndex.C;
        private const int defaultWaitWorker = 2400;
        private const int tickCheckInterval = 60;

        public JobDriver_HaulWithAnimalCart() : base() { }

        public override string GetReport()
        {
            Thing hauledThing = null;
            hauledThing = TargetThingA;
            if (TargetThingA == null)  //Haul Cart
                hauledThing = CurJob.targetC.Thing;
            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            SlotGroup destGroup = null;

            if (pawn.jobs.curJob.targetB != null)
            {
                destLoc = pawn.jobs.curJob.targetB.Cell;
                destGroup = StoreUtility.GetSlotGroup(destLoc);
            }

            if (destGroup != null)
                destName = destGroup.parent.SlotYielderLabel();

            string repString;
            if (destName != null)
                repString = "ReportHaulingTo".Translate(hauledThing.LabelCap, destName);
            else
                repString = "ReportHauling".Translate(hauledThing.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Vehicle_Cart cart = CurJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
            Job jobNew = new Job();


            ///
            //Set fail conditions
            ///

            this.FailOnDestroyed(CarrierInd);
            this.FailOn(() => !cart.mountableComp.IsMounted);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(CarrierInd);


            ///
            //Define Toil
            ///

            Toil extractA = new Toil();
            extractA.initAction = () =>
            {
                if (!CurJob.targetQueueA.NullOrEmpty())
                {
                    CurJob.targetA = CurJob.targetQueueA.First();
                    CurJob.targetQueueA.RemoveAt(0);
                }
            };

            Toil extractB = new Toil();
            extractB.initAction = () =>
            {
                if (!CurJob.targetQueueB.NullOrEmpty())
                {
                    CurJob.targetB = CurJob.targetQueueB.First();
                    CurJob.targetQueueB.RemoveAt(0);
                }
            };

            Toil toilCheckDuplicates = new Toil();
            toilCheckDuplicates.initAction = () =>
            {
                Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position,
                                                                        ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                        PathEndMode.Touch,
                                                                        TraverseParms.For(pawn, Danger.Some),
                                                                        3,
                                                                        t => t.def.defName == CurJob.targetA.Thing.def.defName && !CurJob.targetQueueA.Contains(t));
                if (thing != null && cart.storage.Count + CurJob.targetQueueA.Count < cart.MaxItem
                    && cart.storage.TotalStackCount + CurJob.targetQueueA.Sum(t => t.Thing.stackCount) < cart.GetMaxStackCount && Find.Reservations.CanReserve(pawn, thing))
                {
                    CurJob.targetQueueA.Add(thing);
                    Find.Reservations.Reserve(pawn, thing);
                    JumpToToil(extractA);
                }
            };


            Toil toilCallAnimalCartInThing = new Toil();
            toilCallAnimalCartInThing.initAction = () =>
            {
                jobNew = new Job(DefDatabase<JobDef>.GetNamed("Standby"), CurJob.GetTarget(HaulableInd), defaultWaitWorker);
                cart.mountableComp.Driver.jobs.StartJob(jobNew, JobCondition.InterruptForced);
            };
            toilCallAnimalCartInThing.FailOnDestroyed(HaulableInd);

            Toil toilCallAnimalCartInCell = new Toil();
            toilCallAnimalCartInCell.initAction = () =>
	        {
                jobNew = new Job(DefDatabase<JobDef>.GetNamed("Standby"), CurJob.GetTarget(StoreCellInd), defaultWaitWorker);
                cart.mountableComp.Driver.jobs.StartJob(jobNew, JobCondition.InterruptForced);  
	        };


            Toil toilWaitAnimalCart = new Toil();
            int tickTime = 0;
            toilWaitAnimalCart.initAction = () =>
            {
                Pawn actor = CurToil.actor;
                tickTime = 0;
                if (cart.mountableComp.IsMounted)
                {
                    //Worker is arrival and Animal cart is coming
                    if (cart.mountableComp.Driver.CurJob.JobIsSameAs(jobNew) && !actor.Position.AdjacentTo8WayOrInside(cart))
                        tickTime = 0;
                    //Worker is arrival and Animal cart is arrival
                    else if (cart.mountableComp.Driver.CurJob.JobIsSameAs(jobNew) && actor.Position.AdjacentTo8WayOrInside(cart))
                        ReadyForNextToil();
                    //Worker is arrival but Animal cart is missing
                    else
                    {
                        jobNew = new Job(DefDatabase<JobDef>.GetNamed("Standby"), GetActor().jobs.curJob.GetTarget(HaulableInd), defaultWaitWorker);
                        cart.mountableComp.Driver.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    }
                }
                else
                    EndJobWith(JobCondition.Incompletable);
            };
            toilWaitAnimalCart.tickAction = () => 
            {
                Pawn actor = CurToil.actor;
                tickTime++;
                if (tickTime % tickCheckInterval == 0)
                    if (cart.mountableComp.IsMounted)
                    {
                        //Animal cart is arrival
                        if (cart.mountableComp.Driver.CurJob.JobIsSameAs(jobNew) && actor.Position.AdjacentTo8WayOrInside(cart))
                            ReadyForNextToil();
                        //Animal cart would never come. Imcompletable.
                        else if (!cart.mountableComp.Driver.CurJob.JobIsSameAs(jobNew) || tickTime >= defaultWaitWorker)
                            EndJobWith(JobCondition.Incompletable);
                    }
                    else
                        EndJobWith(JobCondition.Incompletable);
            };
            toilWaitAnimalCart.defaultCompleteMode = ToilCompleteMode.Never;

            Toil toilEnd = new Toil();
            toilEnd.initAction = () =>
            {
                if (cart.mountableComp.IsMounted && cart.mountableComp.Driver.CurJob.JobIsSameAs(jobNew))
                    cart.mountableComp.Driver.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            };

            Toil toilCheckStoreCellEmpty = Toils_Jump.JumpIf(toilEnd, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil toilCheckHaulableEmpty = Toils_Jump.JumpIf(toilCheckStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());

            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CarrierInd);
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            
            yield return Toils_Goto.GotoThing(CarrierInd, PathEndMode.Touch)
                                        .FailOn(() => cart.Destroyed || !cart.TryGetComp<CompMountable>().IsMounted);
            
            //yield return Toils_Jump.JumpIf(toilCheckStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());
            yield return toilCheckHaulableEmpty;

            //Collect TargetQueue
            {

                //Extract an haulable into TargetA
                yield return extractA;

                yield return toilCallAnimalCartInThing;

                yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                                              .FailOnDestroyed(HaulableInd);

                yield return toilWaitAnimalCart;

                yield return ToolsForHaul.Toils_Collect.CollectInCarrier(CarrierInd, HaulableInd);
 
                yield return toilCheckDuplicates;

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            //Toil toilCheckStoreCellEmpty = Toils_Jump.JumpIf(toilDismount, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            yield return toilCheckStoreCellEmpty;

            //Drop TargetQueue
            {
                //Extract an haulable into TargetA
                yield return extractB;

                yield return toilCallAnimalCartInCell;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

                yield return toilWaitAnimalCart;

                yield return ToolsForHaul.Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, CarrierInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            yield return toilEnd;
        }

    }
}
