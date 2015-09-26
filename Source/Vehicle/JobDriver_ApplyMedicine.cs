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
    public class JobDriver_ApplyMedicine : JobDriver
    {
        private const int BaseTreatmentDuration = 600;

        protected Thing Medicine
        {
            get
            {
                return this.CurJob.targetB.Thing;
            }
        }

        protected Pawn Deliveree
        {
            get
            {
                return (Pawn)this.CurJob.targetA.Thing;
            }
        }

        public JobDriver_ApplyMedicine() : base() { }

        public override string GetReport()
        {
            string repString;
            repString = "TreatPatient.reportString".Translate(TargetThingA.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            //Set fail conditions
            ///

            this.FailOnDestroyed(TargetIndex.A);
            this.AddEndCondition(() => { return this.Deliveree.health.ShouldBeTreatedNow ? JobCondition.Ongoing : JobCondition.Succeeded; });
            //Note we only fail on forbidden if the target doesn't start that way
            //This helps haul-aside jobs on forbidden items



            ///
            //Define Toil
            ///



            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(TargetIndex.A);


            StatWorker statWorker = new StatWorker();
            statWorker.InitSetStat(StatDefOf.BaseHealingQuality);

            Toil toilApplyMedicine = new Toil();
            toilApplyMedicine.initAction = () =>
            {
                Thing dummy;
                Medicine.holder.TryDrop(Medicine, pawn.Position + IntVec3.North.RotatedBy(pawn.Rotation), ThingPlaceMode.Direct, out dummy);
            };
            yield return toilApplyMedicine;

            yield return Toils_Treat.PickupMedicine(TargetIndex.B, Deliveree);

            Toil toilGoTodeliveree = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return toilGoTodeliveree;

            int duration = (int) (1.0 / (double) StatExtension.GetStatValue((Thing) pawn, StatDefOf.HealingSpeed, true) * 600.0);
            Toil toilDelivereeWait = new Toil();
            toilDelivereeWait.initAction = () =>
            {
                Deliveree.drafter.TakeOrderedJob(new Job(JobDefOf.Wait, duration));
            };

            yield return Toils_General.Wait(duration);

            yield return Toils_Treat.FinalizeTreatment(Deliveree);

            yield return Toils_Jump.Jump(toilGoTodeliveree);
        }

    }
}
