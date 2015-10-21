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
    public class JobDriver_DismountInBase : JobDriver
    {
        //Constants
        private const TargetIndex CartInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;

        public JobDriver_DismountInBase() : base() { }

        public override string GetReport()
        {
            Vehicle_Cart cart = TargetThingA as Vehicle_Cart;

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
                repString = "ReportDismountingOn".Translate(cart.LabelCap, destName);
            else
                repString = "ReportDismounting".Translate(cart.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnDestroyed(CartInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(CartInd);

            Vehicle_Cart cart = TargetThingA as Vehicle_Cart;


            ///
            //Define Toil
            ///

            Toil toilGoToCell = Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

            ///
            //Toils Start
            ///


            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CartInd);
            yield return Toils_Reserve.Reserve(StoreCellInd);

            //JumpIf already mounted
            yield return Toils_Jump.JumpIf(toilGoToCell, () => { return (cart.GetComp<CompMountable>().Driver == pawn) ? true : false; });

            //Mount on Target
            yield return Toils_Goto.GotoThing(CartInd, PathEndMode.ClosestTouch)
                                        .FailOnDespawned(CartInd);
            yield return Toils_Cart.MountOn(CartInd);

            //Dismount
            yield return toilGoToCell;

            yield return Toils_Cart.DismountAt(CartInd, StoreCellInd);
        }

    }
}
