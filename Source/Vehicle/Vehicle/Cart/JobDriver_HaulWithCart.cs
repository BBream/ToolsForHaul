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
    public class JobDriver_HaulWithCart : JobDriver
    {
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;
        private const TargetIndex CarrierInd = TargetIndex.C;

        public JobDriver_HaulWithCart() : base() { }

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

            ///
            //Set fail conditions
            ///

            this.FailOnDestroyed(CarrierInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(CarrierInd);


            ///
            //Define Toil
            ///

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
            {
                cart.GetComp<CompMountable>().MountOn(pawn);

                //If Driver is not same with worker, this job is failed
                if (cart.GetComp<CompMountable>().Driver != toilMountOn.actor)
                    EndJobWith(JobCondition.Incompletable);
            };

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

            Toil toilDismount = new Toil();
            toilDismount.initAction = () => 
            {
                cart.GetComp<CompMountable>().DismountAt(CurJob.GetTarget(StoreCellInd).Cell);
                Find.Reservations.ReleaseAllClaimedBy(pawn);
            };

            Toil toilFindStoreCellIfNotHomeRegion = new Toil();
            toilFindStoreCellIfNotHomeRegion.initAction = () =>
            {
                IntVec3 storeCell = IntVec3.Invalid;
                if (!Find.AreaHome.ActiveCells.Contains(cart.Position) || cart.Position.GetZone() != null)
                    foreach (IntVec3 cell in Find.AreaHome.ActiveCells.Where(cell => cell.GetZone() == null && cell.Standable()))
                    {
                        if ((cart.Position - cell).LengthHorizontalSquared < (cart.Position - storeCell).LengthHorizontalSquared)
                            storeCell = cell;
                    }
                if (storeCell != IntVec3.Invalid)
                    CurJob.targetB = storeCell;
                else
                    CurJob.targetB = cart.Position;
            };

            Toil toilCheckStoreCellEmpty = Toils_Jump.JumpIf(toilDismount, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil toilCheckHaulableEmpty = Toils_Jump.JumpIf(toilCheckStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());

            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CarrierInd);
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            
            //JumpIf already mounted
            yield return Toils_Jump.JumpIf(toilCheckHaulableEmpty, () => { return (cart.GetComp<CompMountable>().Driver == pawn)? true: false;});
            
            //Mount on Target
            yield return Toils_Goto.GotoThing(CarrierInd, PathEndMode.ClosestTouch)
                                        .FailOnDestroyed(CarrierInd);
            yield return toilMountOn;

            //yield return Toils_Jump.JumpIf(toilCheckStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());
            yield return toilCheckHaulableEmpty;

            //Collect TargetQueue
            {

                //Extract an haulable into TargetA
                yield return extractA;

                yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                                              .FailOnDestroyed(HaulableInd);

                yield return ToolsForHaul.Toils_Collect.CollectInCarrier(CarrierInd, HaulableInd);
 
                yield return toilCheckDuplicates;

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            //Toil toilCheckStoreCellEmpty = Toils_Jump.JumpIf(toilDismount, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            yield return toilCheckStoreCellEmpty;

            //Drop TargetQueue
            {
                //Extract an haulable into TargetB
                yield return extractB;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

                yield return ToolsForHaul.Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, CarrierInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            yield return toilFindStoreCellIfNotHomeRegion;

            yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.OnCell);

            yield return toilDismount;
        }

    }
}
