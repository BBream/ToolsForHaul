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
    public class Vehicle_Cart : ThingWithComps, IThingContainerOwner
    {

        #region Variables
        // ==================================
        private const int MaxItemPerBodySize = 4;
        private const int DefaultMaxItem = 4;

        //Graphic data
        private Graphic_Multi graphic_Handle;
        private Graphic_Multi graphic_Wheel;
        private Graphic_Multi graphic_FullStorage;
        //Body and part location
        private Vector3 handleLoc;
        private Vector3 wheelLoc;
        private Vector3 bodyLoc;
        private int wheelRotation;

        //mount and storage data
        public CompMountable mountableComp;
        public ThingContainer storage = null;
        public ThingContainer GetContainer() { return storage; }
        public IntVec3 GetPosition() { return this.Position; }

        //slotGroupParent Interface
        public ThingFilter allowances;

        public int MaxItem 
        { 
            get 
            { 
                return (mountableComp.IsMounted && mountableComp.Driver.RaceProps.Animal)? 
                    Mathf.CeilToInt(mountableComp.Driver.BodySize * MaxItemPerBodySize) : DefaultMaxItem; 
            } 
        }
        public int GetMaxStackCount { get { return MaxItem * 100; } }

        #endregion

        #region Setup Work

        public Vehicle_Cart():base()
        {
            wheelRotation = 0;

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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
                yield return baseGizmo;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            Action action_Order;

            // do nothing if not of colony
            if (myPawn.Faction != Faction.OfColony)
                yield break;

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            action_Order = () =>
            {
                Find.Reservations.ReleaseAllForTarget(this);
                Find.Reservations.Reserve(myPawn, this);
                Find.DesignationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                Job job = new Job(JobDefOf.Deconstruct, this);
                myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
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

        #endregion

        #region Ticker
        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override void Tick()
        {
            base.Tick();
            if (mountableComp.IsMounted && mountableComp.Driver.pather.Moving && !mountableComp.Driver.stances.FullBodyBusy)
                wheelRotation++;
        }

        #endregion

        #region Graphics / Inspections
        // ==================================

        private void UpdateGraphics()
        {
            graphic_Wheel = new Graphic_Multi();
            graphic_Wheel = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_Wheel", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
            graphic_Handle = new Graphic_Multi();
            graphic_Handle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_Handle", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
            graphic_FullStorage = new Graphic_Multi();
            graphic_FullStorage = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_FullStorage", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
        }

        public override Graphic Graphic
        {
            get
            {
                if (graphic_FullStorage == null)
                    UpdateGraphics();
                return (this.storage.Count > 0)? graphic_FullStorage : base.Graphic;
            }
        }
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
            wheelLoc = drawLoc; wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.04f;
            bodyLoc = drawLoc; bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.03f;

            //Vertical
            if (this.Rotation.AsInt % 2 == 0)
            {
                wheelLoc.z += 0.025f * Mathf.Sin((wheelRotation * 0.10f) % (2 * Mathf.PI));
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.02f;
            }

            base.DrawAt(bodyLoc);
            //Wheels
            //Horizen
            if (this.Rotation.AsInt % 2 == 1)
            {
                Vector2 drawSize = this.def.graphic.drawSize;
                int flip = (this.Rotation == Rot4.West) ? -1 : 1;
                Vector3 scale = new Vector3(1f * drawSize.x, 1f, 1f * drawSize.y);
                Matrix4x4 matrix = new Matrix4x4();
                Vector3 offset = new Vector3(-20f / 192f * drawSize.x * flip, 0, -24f / 192f * drawSize.y);
                Quaternion quat = this.Rotation.AsQuat;
                float x = 1f * Mathf.Sin((flip * wheelRotation * 0.05f) % (2 * Mathf.PI));
                float y = 1f * Mathf.Cos((flip * wheelRotation * 0.05f) % (2 * Mathf.PI));
                quat.SetLookRotation(new Vector3(x, 0, y), Vector3.up);
                matrix.SetTRS(wheelLoc + offset, quat, scale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, graphic_Wheel.MatAt(this.Rotation), 0);
            }
            else
                graphic_Wheel.Draw(wheelLoc, this.Rotation, this);
            graphic_Handle.Draw(handleLoc, this.Rotation, this);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine("InStorage".Translate());
            foreach (Thing thing in this.storage)
                stringBuilder.Append(thing.LabelCap.Translate() + ", ");
            stringBuilder.Remove(stringBuilder.Length - 3, 1);
            return stringBuilder.ToString();
        }
        #endregion
    }
}