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

        public Thing cart;

        public Designator_Mount(): base()
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
            return new AcceptanceReport(txtCannotMount.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfColony || (pawn.RaceProps.Animal && pawn.drafter != null)))
                {
                    Pawn driver = pawn;
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"));
                    Find.Reservations.ReleaseAllForTarget(cart);
                    jobNew.targetA = cart;
                    driver.drafter.TakeOrderedJob(jobNew);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}