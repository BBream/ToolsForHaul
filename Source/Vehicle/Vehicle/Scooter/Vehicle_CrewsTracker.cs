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
    public class Vehicle_CrewsTracker : IExposable, IThingContainerOwner
    {
        public Vehicle vehicle;
        public ThingContainer container;

        public ThingContainer GetContainer() { return this.container; }
        public IntVec3 GetPosition() { return this.vehicle.Position; }
        public bool HasCrew { get { return this.container.Count > 0; } }
        public List<Thing> Crews { get { return this.container.Where(x => x is Pawn).ToList(); } }
        public int CrewsCount { get { return this.container.Count(x => x is Pawn); } }

        public Vehicle_CrewsTracker(Vehicle vehicle)
        {
            this.vehicle = vehicle;
            this.container = new ThingContainer((IThingContainerOwner)this, false);
        }

        public void ExposeData()
        {
            Scribe_Deep.LookDeep<ThingContainer>(ref this.container, "container");
        }

        public void CrewsTick()
        {
            this.container.ThingContainerTick();
        }

        public void BoardOn(Pawn pawn)
        {
            if (this.container.Count(x => x is Pawn) >= vehicle.vehicleDef.vehicle.maxNumOfBoarding //No Space
                || (vehicle.Faction != null && vehicle.Faction != pawn.Faction))                        //Not your vehicle
                return;

            if (pawn.Faction == Faction.OfColony && (pawn.needs.food.CurCategory == HungerCategory.Starving || pawn.needs.rest.CurCategory == RestCategory.Exhausted))
            {
                Messages.Message(pawn.LabelCap + "cannot board on " + vehicle.LabelCap + ": " + pawn.LabelCap + "is starving or exhausted", MessageSound.RejectInput);
                return;
            }

            if (this.container.TryAdd(pawn))
            {
                pawn.holder = this.GetContainer();
                pawn.holder.owner = this;
            }
        }
        public void Unboard(Pawn pawn)
        {
            if (vehicle.vehicleDef.vehicle.maxNumOfBoarding <= 0 && this.container.Count(x => x is Pawn) <= 0)
                return;

            Thing dummy;
            if (this.container.Contains(pawn))
                this.container.TryDrop(pawn, vehicle.MountPos, ThingPlaceMode.Near, out dummy);
        }
        public void UnboardAll()
        {
            if (vehicle.vehicleDef.vehicle.maxNumOfBoarding <= 0 && this.container.Count(x => x is Pawn) <= 0)
                return;

            Thing dummy;
            foreach (Pawn crew in this.container.Where(x => x is Pawn).ToList())
                this.container.TryDrop(crew, vehicle.MountPos, ThingPlaceMode.Near, out dummy);
        }
    }
}
