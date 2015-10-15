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
    class CompEngine : ThingComp
    {

        protected Pawn engine = null;

        public Pawn Engine { get { return engine; } }

        public CompEngine()
        {

        }

        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();
            engine = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("ScooterEngine"), parent.Faction, true);
            engine.training = null;
            engine.pather = new Pawn_PathFollower(engine);
            engine.needs = new Pawn_NeedsTracker(engine);
            engine.inventory = new Pawn_InventoryTracker(engine);
            engine.drafter = new Pawn_DraftController(engine);
            GenSpawn.Spawn(engine, parent.Position);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.LookDeep<Pawn>(ref engine, "engine");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Engine.Faction != parent.Faction)
                Engine.SetFactionDirect(parent.Faction);
        }
    }
}
