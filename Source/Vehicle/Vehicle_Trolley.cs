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
    public class Vehicle_Trolley : ThingWithComps, SlotGroupParent
    {

        #region Variables
        // ==================================
        private static readonly int vehicleGroupKey = 1;

        public SlotGroup slotGroup;
        public StorageSettings settings;
        private List<IntVec3> cachedOccupiedCells;

        //Graphic data
        private Graphic_Multi graphic_Handle;
        private Graphic_Multi graphic_Wheel;
        //Body and part location
        private Vector3 handleLoc;
        private Vector3 wheelLoc;
        private Vector3 bodyLoc;


        //mount and storage data
        public CompMountable mountableComp;

        int tickTime = 0;

        #endregion



        #region Setup Work

        public StorageSettings GetStoreSettings() { return this.settings; }
        public StorageSettings GetParentStoreSettings() { return this.def.building.fixedStorageSettings; }
        public string SlotYielderLabel() { return this.LabelCap; }
        public SlotGroup GetSlotGroup() { return this.slotGroup; }
        public virtual void Notify_ReceivedThing(Thing newItem) {}
        public virtual void Notify_LostThing(Thing newItem) {}

        public virtual IEnumerable<IntVec3> AllSlotCells()
        {
            //This is temporary code. It fit just special case.

            IntVec2 size = this.def.size;
            IntVec3 cell = this.Position;
            cell.x += size.x - 1; cell.z += size.z - 1;

            yield return this.Position;
            yield return cell;
        }

        public List<IntVec3> AllSlotCellsList()
        {
          //if (this.cachedOccupiedCells == null)
          //  this.cachedOccupiedCells = Enumerable.ToList<IntVec3>(this.AllSlotCells());
          return Enumerable.ToList<IntVec3>(this.AllSlotCells());
        }

        public override void PostMake()
        {
          base.PostMake();
          this.settings = new StorageSettings((SlotGroupParent) this);
          if (this.def.building.defaultStorageSettings == null)
            return;
          this.settings.CopyFrom(this.def.building.defaultStorageSettings);
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            //Inventory Initialize
            mountableComp = base.GetComp<CompMountable>();
            this.slotGroup = new SlotGroup((SlotGroupParent)this);
            this.cachedOccupiedCells = Enumerable.ToList<IntVec3>(this.AllSlotCells());

            UpdateGraphics();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep<StorageSettings>(ref this.settings, "settings", (object)this);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
                yield return baseGizmo;

            Command_Action com = new Command_Action();

            com.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconIgnoreForbidden");
            com.defaultLabel = "Store here";
            com.defaultDesc = "Store here";
            com.hotKey = KeyBindingDef.Named("CommandToggleIgnoreForbidden");
            //com.groupKey = vehicleGroupKey;
            com.activateSound = SoundDef.Named("Click");
            com.action = () => 
            {
                Pawn pawn = Find.ListerPawns.FreeColonistsSpawned.First();
                Job jobNew = new Job(JobDefOf.HaulToCell, ListerHaulables.ThingsPotentiallyNeedingHauling().First(), AllSlotCellsList().First());
                pawn.playerController.TakeOrderedJob(jobNew);
            };

            yield return com;
        }

        public override IEnumerable<FloatMenuOption> FloatMenuOptions(Pawn myPawn)
        {
            // do nothing if not of colony
            if (myPawn.Faction != Faction.OfColony)
                yield break;

            foreach (FloatMenuOption fmo in base.FloatMenuOptions(myPawn))
                yield return fmo;

            foreach (var compFMO in mountableComp.CompGetFloatMenuOptionsForExtra(myPawn))
                yield return compFMO;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.slotGroup != null)
                this.slotGroup.Notify_ParentDestroying();
            base.Destroy();
        }

        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            //graphic_Wheel = new Graphic_Multi();
            //graphic_Wheel = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo_Wheel", def.shader, def.DrawSize, def.defaultColor, def.defaultColorTwo) as Graphic_Multi;
            //graphic_Handle = new Graphic_Multi();
            //graphic_Handle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo_Handle", def.shader, def.DrawSize, def.defaultColor, def.defaultColorTwo) as Graphic_Multi;
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
            mountableComp.CompTick();
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
                if (!mountableComp.IsMounted)
                    return base.DrawPos;
                return mountableComp.Position;
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            base.DrawAt(drawLoc);
        }

        #endregion
    }
}