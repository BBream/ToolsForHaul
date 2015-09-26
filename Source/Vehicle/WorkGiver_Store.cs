using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_Store : WorkGiver_Scanner
    {
        public List<Thing> availableVehicle;

        public WorkGiver_Store() : base() { ;}
        /*
        public virtual PathEndMode PathEndMode { get; }
        public virtual ThingRequest PotentialWorkThingRequest { get; }

        public virtual bool HasJobOnCell(Pawn pawn, IntVec3 c);
        public virtual bool HasJobOnThing(Pawn pawn, Thing t);
        public virtual Job JobOnCell(Pawn pawn, IntVec3 cell);
        public virtual Job JobOnThing(Pawn pawn, Thing t);
        public PawnActivityDef MissingRequiredActivity(Pawn pawn);
        public virtual IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn);
        public virtual IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn Pawn);
        public virtual bool ShouldSkip(Pawn pawn);
         */

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ListerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            return true;
            //if (pawn.apparel.WornApparel.Find(ap => ap.def.defName == "Apparel_Backpack");
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            return null;
        }
    }

}