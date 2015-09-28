using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace Vehicle
{
    public class Designator_Move : Designator
    {
        private const string txtCannotMove = "CannotMove";

        public Vehicle vehicle;

        public Designator_Move()
            : base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 1; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (loc.CanReach(vehicle, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly))
                return true;
            else
                return new AcceptanceReport(txtCannotMove.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            vehicle.autoDismountTick = vehicle.thresholdAutoDismount;
            Job jobNew = new Job(JobDefOf.Goto, c);
            vehicle.drafter.TakeOrderedJob(jobNew);

            DesignatorManager.Deselect();
        }
    }
}