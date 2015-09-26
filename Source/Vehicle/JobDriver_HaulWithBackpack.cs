using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace ToolsForHaul
{
    public class JobDriver_HaulWithBackpack : JobDriver
    {
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;
        private const TargetIndex CarrierInd = TargetIndex.C;

        public JobDriver_HaulWithBackpack() : base() { }

        public override string GetReport()
        {
            Thing hauledThing = null;
            hauledThing = TargetThingA;
            IntVec3 destLoc = new IntVec3(-1000, -1000, -1000);
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
            Apparel_Backpack backpack = CurJob.GetTarget(CarrierInd).Thing as Apparel_Backpack;
            Thing lastItem = pawn.inventory.container.Last();

            ///
            //Set fail conditions
            ///

            //this.FailOnDestroyed(HaulableInd);
            //this.FailOnBurningImmobile(StoreCellInd);

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
                                                                        TraverseParms.For(pawn),
                                                                        3,
                                                                        t => t.def.defName == CurJob.targetA.Thing.def.defName && !CurJob.targetQueueA.Contains(t));
                if (thing != null && pawn.inventory.container.Count + CurJob.targetQueueA.Count < backpack.maxItem
                    && pawn.inventory.container.TotalStackCount + CurJob.targetQueueA.Sum(t => t.Thing.stackCount) < backpack.maxItem * 100 && Find.Reservations.CanReserve(pawn, thing))
                {
                    CurJob.targetQueueA.Add(thing);
                    Find.Reservations.Reserve(pawn, thing);
                    JumpToToil(extractA);
                }
            };

            Toil toilEnd = new Toil();
            toilEnd.initAction = () => {};

            Toil toilCheckStoreCellEmpty = Toils_Jump.JumpIf(toilEnd, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil toilCheckHaulableEmpty = Toils_Jump.JumpIf(toilCheckStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());

            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            //yield return Toils_Jump.JumpIf(toilCheckStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());
            yield return toilCheckHaulableEmpty;

            //Collect TargetQueue
            {

                //Extract an haulable into TargetA
                yield return extractA;

                yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                                              .FailOnDestroyed(HaulableInd);

                //yield return Toils_Haul.StartCarryThing(HaulableInd);

                //It won't put off human corpse.
                //yield return Toils_General.PutCarriedThingInInventory();

                //CollectIntoCarrier
                yield return ToolsForHaul.Toils_Collect.CollectInInventory(HaulableInd);
 
                yield return toilCheckDuplicates;

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            //Toil toilCheckStoreCellEmpty = Toils_Jump.JumpIf(toilEnd, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            yield return toilCheckStoreCellEmpty;

            //Drop TargetQueue
            {
                //Extract an haulable into TargetA
                yield return extractB;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

                //CollectIntoCarrier
                yield return ToolsForHaul.Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, lastItem);
                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            yield return toilEnd;
        }

    }
}

