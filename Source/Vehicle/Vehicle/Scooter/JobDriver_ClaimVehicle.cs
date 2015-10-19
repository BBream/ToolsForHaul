using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicle
{
    class JobDriver_ClaimVehicle : JobDriver
    {
        //Constants
        private const TargetIndex TargetAInd = TargetIndex.A;
        private const int TickForClaim = 480;

        private bool claiming = false;

        public JobDriver_ClaimVehicle() : base() { }

        public override string GetReport()
        {
            string repString;
            if (claiming)
                repString = "ReportClaiming".Translate(TargetThingA.LabelCap) + " in " + ((float)(this.ticksLeftThisToil/60f)).ToString("F1") + " second";
            else
                repString = "ReportClaiming".Translate(TargetThingA.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnBurningImmobile(TargetAInd);
            this.FailOnDestroyed(TargetAInd);

            ///
            //Define Toil
            ///




            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            //yield return Toils_Reserve.Reserve(MountableInd, ReservationType.Total);

            //Mount on Target
            yield return Toils_Goto.GotoThing(TargetAInd, PathEndMode.ClosestTouch);

            Toil toilClaim = new Toil();
            toilClaim.initAction = () =>
            {
                claiming = true;
            };
            toilClaim.AddFinishAction(() =>
            {
                Pawn actor = toilClaim.actor;
                TargetThingA.SetFaction(actor.Faction);
            });
            toilClaim.defaultCompleteMode = ToilCompleteMode.Delay;
            toilClaim.defaultDuration = TickForClaim;
            yield return toilClaim;
        }

    }
}

