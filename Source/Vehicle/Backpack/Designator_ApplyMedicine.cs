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
    public class Designator_ApplyMedicine : Designator
    {
        private const string txtNoNeedTreatment = "NoNeedTreatment";

        public Thing medicine;
        public Pawn doctor;
        public Designation designation;

        public Designator_ApplyMedicine()
            : base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 1; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList();

            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.health.ShouldBeTreatedNow)
                    return true;
            }
            return new AcceptanceReport(txtNoNeedTreatment.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.health.ShouldBeTreatedNow)
                {
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("ApplyMedicine"));
                    jobNew.targetA = pawn;
                    jobNew.targetB = medicine;
                    jobNew.maxNumToCarry = Medicine.GetMedicineCountToFullyHeal(jobNew.targetA.Thing as Pawn);
                    doctor.drafter.TakeOrderedJob(jobNew);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}