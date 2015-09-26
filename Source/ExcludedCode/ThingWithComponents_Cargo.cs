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
    public class Cargo : ThingWithComponents
    {

        #region Variables
        // ==================================
        Graphic_Multi graphic_Body = null;
        Graphic_Multi graphic_Handle = null;
        Graphic_Multi graphic_Wheel = null;

        public ThingContainer mountContainer = null;
        public ThingContainer storage = null;
        protected CompMannable mannableComp;

        int maxItem = 5;
        
        double tick_time = 0;


        #endregion

        #region Setup Work
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            //Inventory Initialize
            this.storage = new ThingContainer();
            mountContainer = new ThingContainer();

            UpdateGraphics();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            Command_Action com = new Command_Action();

            com.defaultLabel = "CommandDismountLabel".Translate();
            //com.icon = ContentFinder<Texture2D>.Get("UI/Commands/");
            com.activateSound = SoundDef.Named("Click");
            com.defaultDesc = "CommandDismountDesc".Translate();
            com.action = () => { this.mountContainer.TryDropAll(this.Position, ThingPlaceMode.Near); };

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

                Action action_OrderToManning = delegate
                {
                    Job jobNew = new Job(JobDefOf.Repair , this);
                    myPawn.playerController.TakeOrderedJob(jobNew);
                };
                yield return new FloatMenuOption("Manning on Cargo", action_OrderToManning);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy();
            this.storage.TryDropAll(this.Position, ThingPlaceMode.Near);
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
            tick_time+= 0.1;
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
            get {
                //return base.Graphic;
                return new Graphic();
            }

        }
        /// <summary>
        /// 
        /// </summary>
        public override void DrawAt(Vector3 drawLoc)
        {
            //base.DrawAt(drawLoc);

            IntRot rotation = base.Rotation;
            Vector3 layer = new Vector3(0, Altitudes.AltitudeFor(AltitudeLayer.Waist) - drawLoc.y, 0);
            Vector3 handleLoc = drawLoc; handleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.01f;
            Vector3 wheelLoc = drawLoc; wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.04f;
            Vector3 bodyLoc = drawLoc; bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.03f;
            Vector3 mountThingLoc = drawLoc; mountThingLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
            Vector3 mountThingOffset = new Vector3(0, 0, 1).RotatedBy(this.Rotation.AsAngle);
            Vector3 itemLoc = drawLoc; itemLoc.y = Altitudes.AltitudeFor(AltitudeLayer.PawnState);
            Vector3 itemOffset = new Vector3(0.02f, 0, 0).RotatedBy(this.Rotation.AsAngle);

            float wheel_shake = (float)((Math.Sin(tick_time) + Math.Abs(Math.Sin(tick_time))) / 40.0);
            wheelLoc.z = wheelLoc.z + wheel_shake;


            if (!this.mountContainer.Empty)
                foreach (var mountThing in this.mountContainer.Contents)
                {
                    Pawn p = (Pawn)mountThing;
                    p.ExposeData();
                    p.Rotation = this.Rotation;
                    p.DrawAt(mountThingLoc + mountThingOffset);
                    p.DrawGUIOverlay();
                }
            if (this.Rotation.AsInt%2 == 0) //Vertical
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.02f;
            graphic_Wheel.Draw(wheelLoc, rotation, this);
            graphic_Body.Draw(bodyLoc, rotation, this);
            graphic_Handle.Draw(handleLoc, rotation, this);


            if (!this.storage.Empty)
            {
                float i = 0f;

                foreach (var thing in this.storage.Contents)
                {
                    i += 0.01f;
                    itemLoc.y = Altitudes.AltitudeFor(AltitudeLayer.PawnState) + i;
                    itemOffset.x += i;
                    thing.Graphic.Draw(itemLoc + itemOffset, rotation, this);
                }
            }
        }

        #endregion
    }
}