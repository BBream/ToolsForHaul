using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace ToolsForHaul
{
    public class Vehicle_Saddle : ThingWithComps, IThingContainerOwner
    {

        #region Variables
        // ==================================
        private const int maxNumBoarding = 1;

        //Graphic data
        private Graphic_Multi graphic_Saddle;
        //Body and part location
        private Vector3 saddleLoc;

        //mount and storage data
        public CompMountable mountableComp;
        public ThingContainer storage = null;
        public ThingContainer GetContainer() { return storage; }
        public IntVec3 GetPosition() { return this.Position; }

        public virtual void BoardOn(Pawn pawn)
        {
            if (this.storage.Count(x => x is Pawn) >= maxNumBoarding //No Space
                || (this.Faction != null && this.Faction != pawn.Faction))                        //Not your vehicle
                return;

            if (pawn.Faction == Faction.OfColony && (pawn.needs.food.CurCategory == HungerCategory.Starving || pawn.needs.rest.CurCategory == RestCategory.Exhausted))
            {
                Messages.Message(pawn.LabelCap + "cannot board on " + this.LabelCap + ": " + pawn.LabelCap + "is starving or exhausted", MessageSound.RejectInput);
                return;
            }

            this.storage.TryAdd(pawn);
            pawn.holder = this.GetContainer();
            pawn.jobs.StartJob(new Job(JobDefOf.WaitCombat));
        }
        public virtual void Unboard(Pawn pawn)
        {
            if (this.storage.Count(x => x is Pawn) <= 0)
                return;

            Thing dummy;
            if (this.storage.Contains(pawn))
            {
                pawn.holder = null;
                pawn.jobs.StopAll();
                this.storage.TryDrop(pawn, this.Position, ThingPlaceMode.Near, out dummy);
            }
        }
        public virtual void UnboardAll()
        {
            if (this.storage.Count(x => x is Pawn) <= 0)
                return;

            Thing dummy;
            foreach (Pawn crew in this.storage.Where(x => x is Pawn))
            {
                crew.holder = null;
                crew.jobs.StopAll();
                this.storage.TryDrop(crew, this.Position, ThingPlaceMode.Near, out dummy);
            }
        }
        #endregion

        #region Setup Work

        public Vehicle_Saddle()
            : base()
        {
            this.storage = new ThingContainer(this);
        }


        public override void SpawnSetup()
        {
            base.SpawnSetup();
            mountableComp = base.GetComp<CompMountable>();

            UpdateGraphics();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep<ThingContainer>(ref storage, "storage");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
                yield return baseGizmo;
            if (mountableComp.IsMounted && this.storage.Count(x => x is Pawn) < maxNumBoarding)
            {
                Designator_Board designatorBoard = new Designator_Board();

                designatorBoard.vehicle = this;
                designatorBoard.defaultLabel = "Ride";
                designatorBoard.defaultDesc = "Ride";
                designatorBoard.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconBoard");
                designatorBoard.activateSound = SoundDef.Named("Click");

                yield return designatorBoard;
            }
            if (mountableComp.IsMounted && this.storage.Count(x => x is Pawn) >= maxNumBoarding)
            {
                Command_Action commandUnboardAll = new Command_Action();

                commandUnboardAll.defaultLabel = "Get off";
                commandUnboardAll.defaultDesc = "Get off";
                commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                commandUnboardAll.activateSound = SoundDef.Named("Click");
                commandUnboardAll.action = () => { this.UnboardAll(); };

                yield return commandUnboardAll;

                Designator_Move designator = new Designator_Move();

                designator.driver = this.mountableComp.Driver;
                designator.defaultLabel = "Move";
                designator.defaultDesc = "Move animal";
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimals");
                designator.activateSound = SoundDef.Named("Click");
                designator.hotKey = KeyBindingDefOf.Misc1;

                yield return designator;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            this.storage.TryDropAll(this.Position, ThingPlaceMode.Near);

            if (mode == DestroyMode.Deconstruct)
                mode = DestroyMode.Kill;
            base.Destroy(mode);
        }

        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            graphic_Saddle = new Graphic_Multi();
            graphic_Saddle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Saddle", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
        }

        #endregion

        #region Ticker
        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override void Tick()
        {
            base.Tick();
            storage.ThingContainerTick();
            foreach (Pawn crew in this.storage.Where(x => x is Pawn))
            {
                if (crew.Downed || crew.Dead)
                    this.Unboard(crew);
                crew.Position = this.Position;
            }
            if (!mountableComp.IsMounted)
                this.UnboardAll();

        }

        #endregion

        #region Graphics / Inspections
        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override Vector3 DrawPos
        {
            get
            {
                if (!mountableComp.IsMounted || !this.SpawnedInWorld)
                    return base.DrawPos;
                return mountableComp.Position;
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            if (!this.SpawnedInWorld)
            {
                base.DrawAt(drawLoc);
                return;
            }
            saddleLoc = drawLoc; saddleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.01f;

            if (mountableComp.IsMounted)
            {
                graphic_Saddle.Draw(saddleLoc, this.Rotation, this);
                Vector3 crewLoc = drawLoc; crewLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                Vector3 crewsOffset = new Vector3(0.25f, 0.02f, -0.25f);
                if (this.Rotation == Rot4.North || this.Rotation == Rot4.South)
                    crewsOffset.x = 0f;
                foreach (Pawn pawn in this.storage.Where(x => x is Pawn))
                {
                    pawn.Rotation = this.Rotation;
                    pawn.DrawAt(crewLoc + crewsOffset.RotatedBy(this.Rotation.AsAngle));
                    Stance_Warmup stance_Warmup = null;
                    if (pawn.stances.curStance is Stance_Warmup && Find.Selector.IsSelected(this))
                    {
                        stance_Warmup = pawn.stances.curStance as Stance_Warmup;
                        float pieSizeFactor;
                        if (stance_Warmup.ticksLeft < 300)
                            pieSizeFactor = 1f;
                        else if (stance_Warmup.ticksLeft < 450)
                            pieSizeFactor = 0.75f;
                        else
                            pieSizeFactor = 0.5f;
                        GenDraw.DrawAimPie((Thing)stance_Warmup.stanceTracker.pawn, stance_Warmup.focusTarg, (int)((double)stance_Warmup.ticksLeft * (double)pieSizeFactor), 0.2f);
                    }
                }
            }
            else
                base.DrawAt(drawLoc);
        }
        #endregion
    }
}