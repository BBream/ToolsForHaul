using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace ToolsForHaul
{
    public class Designator_Move : Designator
    {
        private const string txtCannotMove = "CannotMove";

        public Pawn driver;

        public Designator_Move()
            : base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 0; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (loc.CanReach(driver, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly))
                return true;
            else
                return new AcceptanceReport(txtCannotMove.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Standby"), c, 4800);
            driver.jobs.StartJob(jobNew, JobCondition.Incompletable);

            DesignatorManager.Deselect();
        }
    }
}