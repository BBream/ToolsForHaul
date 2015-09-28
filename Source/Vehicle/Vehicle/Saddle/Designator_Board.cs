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
    class Designator_Board : Designator
    {
        private const string txtCannotBoard = "CannotBoard";

        public Thing vehicle;

        public Designator_Board()
            : base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList();

            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfColony && (pawn.RaceProps.mechanoid || pawn.RaceProps.Humanlike)))
                    return true;
            }
            return new AcceptanceReport(txtCannotBoard.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfColony && (pawn.RaceProps.mechanoid || pawn.RaceProps.Humanlike)))
                {
                    Pawn crew = pawn;
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Board"));
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    jobNew.targetA = vehicle;
                    crew.drafter.TakeOrderedJob(jobNew);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}