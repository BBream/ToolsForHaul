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
    class JobDriver_Standby : JobDriver
    {
        //Constants
        private const TargetIndex DestInd = TargetIndex.A;

        public JobDriver_Standby() : base() { }

        public override string GetReport()
        {
            string repString;
            repString = "ReportStandby".Translate();

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOn(() => !TargetA.Cell.IsValid);
            this.FailOnBurningImmobile(DestInd);

            ///
            //Define Toil
            ///




            ///
            //Toils Start
            ///

            yield return Toils_Goto.GotoCell(DestInd, PathEndMode.ClosestTouch);

            yield return Toils_General.Wait(CurJob.expiryInterval);
        }

    }
}
