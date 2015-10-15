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
    public class JobDriver_Mount : JobDriver
    {
        //Constants
        private const TargetIndex VehicleInd = TargetIndex.A;
        private const TargetIndex MountCellInd = TargetIndex.B;

        public JobDriver_Mount() : base() { }

        public override string GetReport()
        {
            Vehicle vehicle = TargetThingA as Vehicle;

            string repString;
            repString = "ReportMounting".Translate(vehicle.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnBurningImmobile(MountCellInd);
            this.FailOnDestroyed(VehicleInd);
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
                this.FailOnForbidden(VehicleInd);



            ///
            //Define Toil
            ///




            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            //yield return Toils_Reserve.Reserve(MountableInd, ReservationType.Total);

            //Mount on Target
            yield return Toils_Goto.GotoThing(VehicleInd, PathEndMode.ClosestTouch);

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
            {
                Pawn actor = toilMountOn.actor;
                Vehicle vehicle = TargetThingA as Vehicle;
                vehicle.driver.MountOn(actor);
            };

            yield return toilMountOn;
        }

    }
}
