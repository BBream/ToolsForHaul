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
    public class JobDriver_DropInCell : JobDriver
    {
        //Constants
        private const TargetIndex DropThingInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;
        private const TargetIndex CarrierInd = TargetIndex.C;

        public JobDriver_DropInCell() : base() { }

        public override string GetReport()
        {
            Thing hauledThing = TargetThingA;

            IntVec3 destLoc = pawn.jobs.curJob.targetB.Cell;
            string destName = null;
            SlotGroup destGroup = StoreUtility.GetSlotGroup(destLoc);

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
            //Set fail conditions
            this.FailOnBurningImmobile(StoreCellInd);
            this.FailOnDestroyed(CarrierInd);

            ///
            //Toils Start
            ///

            //Reserve target storage cell
            yield return Toils_Reserve.Reserve(StoreCellInd);

            //Drop thing in cell
            yield return Toils_Goto.GotoCell(StoreCellInd, PathMode.ClosestTouch);
            yield return ToolsForHaul.Toils_Collect.DropInCell(DropThingInd, StoreCellInd, CarrierInd, ThingPlaceMode.Direct);
            yield return Toils_General.RemoveDesignationsOnThing(DropThingInd, DesignationDefOf.Haul);
        }

    }
}
