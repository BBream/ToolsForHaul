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

        protected Pawn engine;

        public Pawn Engine { get { return engine; } }

        public CompEngine()
        {
            engine = null;
        }

        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();
            if (engine == null)
            {
                engine = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("ScooterEngine"), parent.Faction, true);
                engine.training = null;
                engine.pather = new Pawn_PathFollower(engine);
                engine.jobs = new Pawn_JobTracker(engine);
                engine.needs = new Pawn_NeedsTracker(engine);
                engine.drafter = new Pawn_DraftController(engine);
            }
            GenSpawn.Spawn(engine, parent.Position);
        }

        public override void PostDeSpawn()
        {
            base.PostDeSpawn();
            if (engine.SpawnedInWorld)
                engine.DeSpawn();
            Find.Reservations.ReleaseAllClaimedBy(engine);
            engine.stances.CancelBusyStanceSoft();
            engine.jobs.StopAll(false);
            engine.pather.StopDead();
            engine.drafter.Drafted = false;
        }

        public override void PostDestroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDestroy(mode);
            if (!engine.Destroyed)
                engine.Destroy(DestroyMode.Vanish);
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
