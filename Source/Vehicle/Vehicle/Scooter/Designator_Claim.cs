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
    class Designator_Claim : Designator
    {
        private const string txtCannotClaim = "CannotClaim";

        public ThingWithComps vehicle;

        public Designator_Claim()
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
                if (pawn != null && (pawn.Faction == Faction.OfColony || (pawn.RaceProps.Animal && pawn.drafter != null)))
                    return true;
            }
            return new AcceptanceReport(txtCannotClaim.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfColony || (pawn.RaceProps.Animal && pawn.drafter != null)))
                {
                    Pawn claiment = pawn;
                    Job job = new Job(DefDatabase<JobDef>.GetNamed("ClaimVehicle"));
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    job.targetA = vehicle;
                    claiment.drafter.TakeOrderedJob(job);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}
