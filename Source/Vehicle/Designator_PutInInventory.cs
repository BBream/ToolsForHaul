using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace ToolsForHaul
{
    public class Designator_PutInInventory : Designator
    {
        private const string txtBackpackIsFull = "BackpackIsFull";
        private const string txtInvalidPutInTarget = "InvalidPutInTarget";

        public Apparel_Backpack backpack;
        public List<Designation> designations;

        private int numOfContents;

        public Designator_PutInInventory() : base()
        {
            useMouseIcon = true;
            designations = new List<Designation>();
            this.soundSucceeded = SoundDefOf.DesignateHaul;
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList();

            numOfContents = backpack.wearer.inventory.container.Count;

            int designationsTotalStackCount = 0;
            foreach (var designation in designations)
                designationsTotalStackCount += designation.target.Thing.stackCount;

            //No Item space or no stack space
            if ((designations.Count + numOfContents) >= backpack.maxItem 
                || (designationsTotalStackCount + backpack.wearer.inventory.container.TotalStackCount) >= backpack.maxStack)
                return new AcceptanceReport(txtBackpackIsFull.Translate());


            foreach (var thing in thingList)
            {
                if (thing.def.category == ThingCategory.Item && !Find.Reservations.IsReserved(thing, Faction.OfColony))
                    return true;
            }
            return new AcceptanceReport(txtInvalidPutInTarget.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (var thing in thingList)
                if (thing.def.category == ThingCategory.Item && !Find.Reservations.IsReserved(thing, Faction.OfColony))
                    designations.Add(new Designation(thing, DesignationDefOf.Haul));
        }

        protected override void FinalizeDesignationSucceeded()
        {
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("PutInInventory"));
            jobNew.maxNumToCarry = 1;
            jobNew.targetA = backpack;
            jobNew.targetQueueB = new List<TargetInfo>();

            while (!designations.NullOrEmpty())
            {
                jobNew.targetQueueB.Add(designations.First().target.Thing);
                designations.RemoveAt(0);
            }
            if (!jobNew.targetQueueB.NullOrEmpty())
                //if (backpack.wearer.drafter.CanTakePlayerJob())
                    backpack.wearer.drafter.TakeOrderedJob(jobNew);
                //else
                //    backpack.wearer.drafter.QueueJob(jobNew);
            DesignatorManager.Deselect();
        }

        public override void SelectedUpdate()
        {
            foreach (var designation in designations)
                designation.DesignationDraw();
        }
    }
}