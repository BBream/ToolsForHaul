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
    public class Vehicle_Cart : ThingWithComps, IThingContainerGiver
    {

        #region Variables
        // ==================================

        //Graphic data
        private Graphic_Multi graphic_Handle;
        private Graphic_Multi graphic_Wheel;
        //Body and part location
        private Vector3 handleLoc;
        private Vector3 wheelLoc;
        private Vector3 bodyLoc;

        //storage item location
        private Vector3 itemLoc;
        private Vector3 itemOffset;


        //mount and storage data
        public CompMountable mountableComp;
        public ThingContainer storage = null;
        public ThingContainer GetContainer() { return storage; }
        public IntVec3 GetPosition() { return this.Position; }

        //slotGroupParent Interface
        public ThingFilter allowances;

        public int maxItem = 6;
        public int GetMaxStackCount { get { return maxItem * 100; } }

        int tickTime = 0;


        #endregion

        #region Setup Work

        public Vehicle_Cart():base()
        {
            //Inventory Initialize. It should be moved in constructor
            this.storage = new ThingContainer(this);
            this.allowances = new ThingFilter();
            this.allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            this.allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
        }


        public override void SpawnSetup()
        {
            base.SpawnSetup();
            mountableComp = base.GetComp<CompMountable>();

            if (allowances == null)
            {
                this.allowances = new ThingFilter();
                this.allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
                this.allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
            }

            UpdateGraphics();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep<ThingContainer>(ref storage, "storage");
            Scribe_Deep.LookDeep<ThingFilter>(ref allowances, "allowances");
        }

        /*public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
                yield return baseGizmo;

            Command_Action com = new Command_Action();
            if (ignoreForbidden)
            {
                com.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconIgnoreForbidden");
                com.defaultLabel = "Ignore Fobidden";
                com.defaultDesc = "Ignore Fobidden";
            }
            else
            {
                com.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconConsiderForbidden");
                com.defaultLabel = "Consider Fobidden";
                com.defaultDesc = "Consider Fobidden";
            }
            com.hotKey = KeyBindingDef.Named("CommandToggleIgnoreForbidden");
            //com.groupKey = vehicleGroupKey;
            com.activateSound = SoundDef.Named("Click");
            com.action = () => { ignoreForbidden = ignoreForbidden ^ true; };// Toggle

            yield return com;
        }*/

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            Action action_Order;

            // do nothing if not of colony
            if (myPawn.Faction != Faction.OfColony)
                yield break;

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            foreach (var compFMO in mountableComp.CompGetFloatMenuOptionsForExtra(myPawn))
                yield return compFMO;

            action_Order = () =>
            {
                Find.Reservations.ReleaseAllForTarget(this);
                Find.Reservations.TryReserve(myPawn, this);
                Find.DesignationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                Job jobNew = new Job(JobDefOf.Deconstruct, this);
                myPawn.playerController.TakeOrderedJob(jobNew);
            };

            yield return new FloatMenuOption("Deconstruct".Translate(this.LabelBase), action_Order);
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
            graphic_Wheel = new Graphic_Multi();
            graphic_Wheel = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_Wheel", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
            graphic_Handle = new Graphic_Multi();
            graphic_Handle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_Handle", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
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
            tickTime += 1;
            //mountableComp.CompTick();
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

            //Body and part location
            handleLoc = drawLoc; handleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.01f;
            wheelLoc = drawLoc; wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.04f;
            bodyLoc = drawLoc; bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.03f;

            //storage item location
            itemLoc = drawLoc; itemLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist);
            itemOffset = new Vector3(0.02f, 0, 0).RotatedBy(this.Rotation.AsAngle);

            if (mountableComp.IsMounted && mountableComp.Driver.pather.Moving)
            {
                float wheel_shake = (float)((Math.Sin(tickTime / 10) + Math.Abs(Math.Sin(tickTime / 10))) / 40.0);
                wheelLoc.z = wheelLoc.z + wheel_shake;
            }

            if (this.Rotation.AsInt % 2 == 0) //Vertical
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.02f;

            base.DrawAt(bodyLoc);
            graphic_Wheel.Draw(wheelLoc, this.Rotation, this);
            graphic_Handle.Draw(handleLoc, this.Rotation, this);


            if (!this.storage.Empty)
            {
                float i = 0f;
                itemOffset.x -= 0.06f;
                foreach (var thing in this.storage.Contents)
                {
                    i += 0.01f;
                    itemLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Waist) + 0.05f + i;
                    itemOffset.x += i * 3;
                    itemOffset.z = (this.Position.z % 10) / 100;
                    itemOffset = itemOffset.RotatedBy(this.Rotation.AsAngle);
                    if ((thing.ThingID.IndexOf("Corpse") <= -1) ? false : true)
                    {
                        Corpse corpse = thing as Corpse;
                        BodyDrawType bodyDrawType = BodyDrawType.Normal;
                        CompRottable rottable = GetComp<CompRottable>();
                        if (rottable != null)
                        {
                            if (rottable.Stage == RotStage.Rotting)
                                bodyDrawType = BodyDrawType.Rotting;
                            else if (rottable.Stage == RotStage.Dessicated)
                                bodyDrawType = BodyDrawType.Dessicated;
                        }
                        corpse.innerPawn.drawer.renderer.RenderPawnAt(itemLoc + itemOffset, bodyDrawType);
                    }
                    else
                        thing.Graphic.Draw(itemLoc + itemOffset, this.Rotation, this);
                }
            }
        }

        #endregion
    }
}