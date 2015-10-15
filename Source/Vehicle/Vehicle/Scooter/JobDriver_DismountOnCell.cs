using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

using Verse;
using Verse.AI;
using RimWorld;


namespace Vehicle
{
    public class JobDriver_DismountOnCell : JobDriver
    {
        //Constants
        private const TargetIndex VehicleInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;

        public JobDriver_DismountOnCell() : base() { }

        public override string GetReport()
        {
            IntVec3 destLoc = pawn.jobs.curJob.targetA.Cell;

            string destName = null;
            SlotGroup destGroup = StoreUtility.GetSlotGroup(destLoc);
            if (destGroup != null)
                destName = destGroup.parent.SlotYielderLabel();

            string repString;
            if (destName != null)
                repString = "DismountingOn".Translate(destLoc);
            else
                repString = "Dismounting";
            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnBurningImmobile(StoreCellInd);

            Vehicle vehicle = CurJob.GetTarget(VehicleInd).Thing as Vehicle;

            this.FailOn(() => { return (vehicle.IsMounted) ? false : true; });


            ///
            //Define Toil
            ///



            Toil toilDismount = new Toil();
            toilDismount.initAction = () => { vehicle.driver.Dismount(); };


            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(StoreCellInd);

            //Dismount
            yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.OnCell);

            yield return toilDismount;
        }

    }
}
