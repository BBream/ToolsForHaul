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
        private const TargetIndex BackpackInd = TargetIndex.C;

        public JobDriver_HaulWithBackpack() : base() { }

        public override string GetReport()
        {
            Thing hauledThing = null;
            hauledThing = TargetThingA;
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
            if (destName != null && hauledThing != null)
                repString = "ReportHaulingTo".Translate(hauledThing.LabelCap, destName);
            else if (hauledThing != null)
                repString = "ReportHauling".Translate(hauledThing.LabelCap);
            else
                repString = "ReportHauling".Translate();
            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Apparel_Backpack backpack = CurJob.GetTarget(BackpackInd).Thing as Apparel_Backpack;
            Thing lastItem = ToolsForHaulUtility.TryGetBackpackLastItem(pawn);

            ///
            //Set fail conditions
            ///

            ///
            //Define Toil
            ///

            Toil endOfJob = new Toil();
                endOfJob.initAction = () => { EndJobWith(JobCondition.Succeeded); };
            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(endOfJob, () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil checkHaulableEmpty = Toils_Jump.JumpIf(checkStoreCellEmpty, () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());

            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            //JumpIf checkStoreCellEmpty
            yield return checkHaulableEmpty;

            //Collect TargetQueue
            {
                Toil extractA = Toils_Collect.Extract(HaulableInd);
                yield return extractA;

                Toil gotoThing = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                                                    .FailOnDestroyed(HaulableInd);
                yield return gotoThing;

                yield return ToolsForHaul.Toils_Collect.CollectInInventory(HaulableInd);

                yield return Toils_Collect.CheckDuplicates(gotoThing, BackpackInd, HaulableInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            //JumpIf toilEnd
            yield return checkStoreCellEmpty;

            //Drop TargetQueue
            {
                Toil extractB = Toils_Collect.Extract(StoreCellInd);
                yield return extractB;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch)
                                            .FailOnBurningImmobile(StoreCellInd);

                yield return ToolsForHaul.Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, lastItem);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            yield return endOfJob;
        }

    }
}

