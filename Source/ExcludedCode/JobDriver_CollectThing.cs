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
    public class JobDriver_CollectThing : JobDriver
    {
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex CarrierInd = TargetIndex.C;

        public JobDriver_CollectThing() : base() { }

        public override string GetReport()
        {
            Thing hauledThing = TargetThingA;

            string repString;
            repString = "ReportHauling".Translate(hauledThing.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnDestroyed(HaulableInd);
            this.FailOnDestroyed(CarrierInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(HaulableInd);



            ///
            //Define Toil
            ///
            Toil toilGoto = null;
            toilGoto = Toils_Goto.GotoThing(HaulableInd, PathMode.ClosestTouch)
                .FailOn(() =>
                {
                    Vehicle_Cargo vc = CurJob.GetTarget(CarrierInd).Thing as Vehicle_Cargo;

                    if (!vc.storage.CanAcceptAnyOf(CurJob.GetTarget(HaulableInd).Thing))
                        return true;
                    return false;
                });



            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(HaulableInd);

            //Collect Target
            yield return toilGoto;
            yield return ToolsForHaul.Toils_Collect.CollectThing(HaulableInd, CarrierInd);
        }

    }
}
