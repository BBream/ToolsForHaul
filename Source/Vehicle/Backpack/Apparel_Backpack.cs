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
    public class Apparel_Backpack : Apparel
    {
        /*
        public Pawn wearer;

        public Apparel();

        public virtual bool AllowVerbCast(IntVec3 root, TargetInfo targ);
        public virtual bool CheckPreAbsorbDamage(DamageInfo dinfo);
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish);
        public virtual void DrawWornExtras();
        public virtual IEnumerable<Gizmo> GetWornGizmos();
        */

        private const string DesignatorPutInInventoryDefaultLabel = "DesignatorPutInDefaultLabel";
        private const string DesignatorPutInInventoryDefaultDesc = "DesignatorPutInDefaultDesc";
        private static readonly StatDef backpackMaxItem = DefDatabase<StatDef>.GetNamed("BackpackMaxItem");

        public int maxItem;
        public Pawn postWearer;
        Gizmo_BackpackEquipment gizmo;
        Designator_PutInInventory designator;

        public int maxStack { get { return maxItem * 1; } }

        public Apparel_Backpack() : base()
        {
            postWearer = null;
            gizmo = new Gizmo_BackpackEquipment();
            designator = new Designator_PutInInventory();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            maxItem = (int)this.GetStatValue(backpackMaxItem);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref maxItem, "maxItem", (int)this.GetStatValue(backpackMaxItem));
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void Tick()
        {
            base.Tick();
            //Put on backpack
            if (postWearer == null && wearer != null)
            {
                postWearer = wearer;
            }

            //Put off backpack. Should drop all from postWearer
            else if (postWearer != null && wearer == null)
            {
                postWearer.inventory.container.TryDropAll(postWearer.Position, ThingPlaceMode.Near);
                postWearer = null;
            }
        }

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            if (designator == null)
                designator = new Designator_PutInInventory();

            designator.backpack = this;
            designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconPutIn");
            designator.defaultLabel = DesignatorPutInInventoryDefaultLabel.Translate() + "(" + wearer.inventory.container.TotalStackCount + "/" + maxStack + ")";
            designator.defaultDesc = DesignatorPutInInventoryDefaultDesc.Translate() + wearer.inventory.container.TotalStackCount + "/" + maxStack;
            designator.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            designator.activateSound = SoundDef.Named("Click");

            yield return designator;

            if (gizmo == null)
                gizmo = new Gizmo_BackpackEquipment();

            gizmo.backpack = this;
            yield return gizmo;
        }
    }
}