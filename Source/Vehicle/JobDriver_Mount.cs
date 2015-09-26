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
    public class JobDriver_Mount : JobDriver
    {
        //Constants
        private const TargetIndex MountableInd = TargetIndex.A;

        public JobDriver_Mount() : base() { }

        public override string GetReport()
        {
            string repString;
            repString = "ReportMounting".Translate(TargetThingA.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnDestroyed(MountableInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(MountableInd);



            ///
            //Define Toil
            ///




            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(MountableInd);

            //Mount on Target
            yield return Toils_Goto.GotoThing(MountableInd, PathEndMode.ClosestTouch);

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
            {
                Pawn actor = toilMountOn.actor;
                Job curJob = actor.jobs.curJob;

                TargetThingA.TryGetComp<CompMountable>().Driver = actor;
            };

            yield return toilMountOn;
        }

    }
}
