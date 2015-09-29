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
    public class Designator_Mount : Designator
    {
        private const string txtCannotMount = "CannotMount";

        public Thing vehicle;

        public Designator_Mount(): base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            Pawn pawn = loc.GetThingList().Find(t => t is Pawn) as Pawn;
            if (pawn == null)
                return new AcceptanceReport(txtCannotMount.Translate() + ": " + "NotPawn".Translate());
            if (pawn.Faction != Faction.OfColony)
                return new AcceptanceReport(txtCannotMount.Translate() + ": " + "NotColonyFaction".Translate());
            if (!pawn.RaceProps.Animal && vehicle is Vehicle_Saddle)
                return new AcceptanceReport(txtCannotMount.Translate() + ": " + "NotHumanlikeOrMechanoid".Translate());
            if (pawn.RaceProps.Animal && !pawn.training.IsCompleted(TrainableDefOf.Obedience))
                return new AcceptanceReport(txtCannotMount.Translate() + ": " + "NotTrainedAnimal".Translate());
            if (pawn.RaceProps.Animal && !(pawn.RaceProps.baseBodySize >= 1.0))
                return new AcceptanceReport(txtCannotMount.Translate() + ": " + "TooSmallAnimal".Translate());
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfColony && (pawn.RaceProps.mechanoid || pawn.RaceProps.Humanlike)))
                {
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"));
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    jobNew.targetA = vehicle;
                    pawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    break;
                }
                else if (pawn != null && (pawn.Faction == Faction.OfColony && pawn.RaceProps.Animal && pawn.training.IsCompleted(TrainableDefOf.Obedience) && pawn.RaceProps.baseBodySize >= 1.0))
                {
                    Pawn worker = null;
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("MakeMount"));
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    jobNew.maxNumToCarry = 1;
                    jobNew.targetA = vehicle;
                    jobNew.targetB = pawn;
                    foreach (Pawn colonyPawn in Find.ListerPawns.FreeColonistsSpawned)
                        if (worker == null || (worker.Position - pawn.Position).LengthHorizontal > (colonyPawn.Position - pawn.Position).LengthHorizontal)
                            worker = colonyPawn;
                    if (worker == null)
                    {
                        Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                        break;
                    }
                    worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    pawn.jobs.StartJob(new Job(DefDatabase<JobDef>.GetNamed("GotoAndWait"), pawn.Position, 2400 + (int)((worker.Position - pawn.Position).LengthHorizontal * 120)));
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}