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
    public class JobDriver_DropAllInCell : JobDriver
    {
        //Constants
        private const TargetIndex StoreCellInd = TargetIndex.A;


        public JobDriver_DropAllInCell() : base() { }

        public override string GetReport()
        {
            Thing hauledThing = pawn.inventory.container.ContentsListForReading.Last();

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

            //Reserve target storage cell
            yield return Toils_Reserve.Reserve(StoreCellInd);

            yield return Toils_Goto.GotoCell(StoreCellInd, PathMode.ClosestTouch);

            yield return ToolsForHaul.Toils_Collect.DropAllInCell(StoreCellInd, ThingPlaceMode.Near);
        }

    }
}
