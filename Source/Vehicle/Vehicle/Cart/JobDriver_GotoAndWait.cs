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
    class JobDriver_GotoAndWait : JobDriver
    {
        //Constants
        private const TargetIndex DestInd = TargetIndex.A;

        public JobDriver_GotoAndWait() : base() { }

        public override string GetReport()
        {
            string repString;
            repString = "ReportGoto".Translate(TargetA.Cell);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnBurningImmobile(DestInd);
            this.FailOn(() => TargetA.Cell == IntVec3.Invalid);

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
