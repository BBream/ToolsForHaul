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
    public class AttachableCargo : AttachableThing
    {

        #region Variables
        // ==================================

        int maxItem = 5;

        double tick_time = 0;


        #endregion

        #region Setup Work
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            //Inventory Initialize
            this.holder = new ThingContainer();
            this.holder.Clear();
            UpdateGraphics();

        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            Command_Action com = new Command_Action();

            com.defaultLabel = "CommandDropAllLabel".Translate();
            //com.icon = ContentFinder<Texture2D>.Get("UI/Commands/");
            com.activateSound = SoundDef.Named("Click");
            com.defaultDesc = "CommandDropAllDesc".Translate();
            com.action = () => { this.holder.TryDropAll(this.Position, ThingPlaceMode.Near); };

            yield return com;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptionsFor(Pawn myPawn)
        {
            // do nothing if not of colony
            if (myPawn.Faction != Faction.OfColony)
                yield break;

            // base float menus
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptionsFor(myPawn))
                yield return fmo;

            // order to drive
            if (myPawn is Pawn)
            {

                //Log.Error(myPawn.healthTracker.GetEfficiency(PawnActivityDefOf.Eating).ToString());
                Action action_OrderToMount = delegate
                {
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"), this);
                    myPawn.playerController.TakeOrderedJob(jobNew);
                };
                yield return new FloatMenuOption("Mount on Cargo", action_OrderToMount);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy();
            this.inventory.DropAll(this.Position);
        }

        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            graphic_Body = new Graphic_Multi();
            graphic_Handle = new Graphic_Multi();
            graphic_Wheel = new Graphic_Multi();

            graphic_Wheel = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo_Wheel", def.shader, def.DrawSize, def.defaultColor, def.defaultColorTwo);
            graphic_Body = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo", def.shader, def.DrawSize, def.defaultColor, def.defaultColorTwo);
            graphic_Handle = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo_Handle", def.shader, def.DrawSize, def.defaultColor, def.defaultColorTwo);
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
            tick_time += 0.1;
        }
        #endregion

        #region Graphics / Inspections
        // ==================================

        /// <summary>
        /// This returns the graphic of the object.
        /// The renderer will draw the needed object graphic from here.
        /// </summary>
        public override Graphic Graphic
        {
            get
            {
                //return base.Graphic;
                return new Graphic();
            }

        }
        /// <summary>
        /// 
        /// </summary>
        public override void DrawAt(Vector3 drawLoc)
        {
            base.DrawAt(drawLoc);

            IntRot rotation = base.Rotation;
            Vector3 drawWheel = drawLoc;
            Vector3 drawBackWheel = drawLoc;
            float wheel_shake = (float)((Math.Sin(tick_time) + Math.Abs(Math.Sin(tick_time))) / 40.0);

            drawWheel.z = drawWheel.z + wheel_shake;

            IntRot pawnDirection = this.Rotation;
            bool pawnIsHorizen = (pawnDirection.AsInt % 2 == 1) ? true : false;

            if (pawnIsHorizen)
            {
                if (!this.mountContainer.Empty)
                    foreach (var mountThing in this.mountContainer.Contents)
                    {
                        Pawn p = (Pawn)mountThing;
                        p.ExposeData();
                        //Graphic_Linked mountGraphic = new Graphic_Linked(p.Graphic);
                        //mountGraphic.Draw(drawLoc, rotation, this);
                        p.DrawAt(drawLoc);

                    }

                graphic_Wheel.Draw(drawWheel, rotation, this);
                graphic_Body.Draw(drawLoc, rotation, this);
                graphic_Handle.Draw(drawLoc, rotation, this);
                if (!this.inventory.container.Empty)
                {
                    foreach (var thing in this.inventory.container.Contents)
                        thing.Graphic.Draw(drawLoc, rotation, this);
                }
            }
            else
            {
                graphic_Body.Draw(drawLoc, rotation, this);
                graphic_Handle.Draw(drawLoc, rotation, this);
                graphic_Wheel.Draw(drawWheel, rotation, this);
                if (!this.inventory.container.Empty)
                {
                    foreach (var thing in this.inventory.container.Contents)
                        thing.Graphic.Draw(drawLoc, rotation, this);
                }
            }
        }

        #endregion
    }
}