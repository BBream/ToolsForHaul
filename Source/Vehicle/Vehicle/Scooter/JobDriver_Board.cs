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
    public class JobDriver_Board : JobDriver
    {
        //Constants
        private const TargetIndex MountableInd = TargetIndex.A;
        private const TargetIndex MountCellInd = TargetIndex.B;

        public JobDriver_Board() : base() { }

        public override string GetReport()
        {
            Vehicle vehicle = TargetThingA as Vehicle;

            string repString;
            repString = "ReportBoarding".Translate(vehicle.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnBurningImmobile(MountCellInd);
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
            //yield return Toils_Reserve.Reserve(MountableInd, ReservationType.Total);

            //Mount on Target
            yield return Toils_Goto.GotoThing(MountableInd, PathEndMode.ClosestTouch);

            Toil toilBoardOn = new Toil();
            toilBoardOn.initAction = () =>
            {
                Pawn actor = toilBoardOn.actor;
                Vehicle vehicle = TargetThingA as Vehicle;
                vehicle.crews.BoardOn(actor);
            };

            yield return toilBoardOn;
        }

    }
}
