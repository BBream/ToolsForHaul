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
    public class Vehicle_DriverTracker : IExposable, IThingContainerOwner
    {
        private Building_Door lastPassedDoor = null;
        private int tickLastDoorCheck = 0;
        private const int TickCooldownDoorCheck = 96;
        private const int tickCheckDriverInterval = 256;

        public Vehicle vehicle;
        public ThingContainer container;

        public ThingContainer GetContainer() { return this.container; }
        public IntVec3 GetPosition() { return this.vehicle.Position; }
        public Pawn Driver { get { return this.container.Count > 0 ? this.container[0] as Pawn : null; } }
        public Pawn DrivingWorker { get { return vehicle.TryGetComp<CompEngine>() != null ? vehicle.TryGetComp<CompEngine>().Engine : Driver; } }
        public bool IsMounted { get { return (Driver != null) ? true : false; } }
        public Vector3 Position
        {
            get
            {
                Vector3 mapSize = Find.Map.Size.ToVector3();
                Vector3 position = DrivingWorker.DrawPos;
                //No engine
                if (DrivingWorker == null)
                    return vehicle.DrawPos;
                //Out of bound or Preventing cart from stucking door
                else if (!GenGrid.InBounds(position))
                    return DrivingWorker.DrawPos;
                else
                    return DrivingWorker.DrawPos;
            }
        }

        public Vehicle_DriverTracker(Vehicle vehicle)
        {
            this.vehicle = vehicle;
            this.container = new ThingContainer((IThingContainerOwner)this, true);
        }

        public void ExposeData()
        {
            Scribe_Deep.LookDeep<ThingContainer>(ref this.container, "container");
            Scribe_References.LookReference<Building_Door>(ref lastPassedDoor, "lastPassedDoor");
        }

        public void DriverTick()
        {
            this.container.ThingContainerTick();
            if (IsMounted)
            {
                if (Driver.Downed || Driver.Dead)
                    this.Dismount();

                if (Find.TickManager.TicksGame % tickCheckDriverInterval == 0
                    && (Driver.needs.food.CurCategory == HungerCategory.Starving || Driver.needs.rest.CurCategory == RestCategory.Exhausted))
                    this.Dismount();
                vehicle.Position = IntVec3Utility.ToIntVec3(Position);
                vehicle.Rotation = DrivingWorker.Rotation;
            }
            ProcessForDoor();
        }

        public void MountOn(Pawn pawn)
        {
            if (this.IsMounted                                             //No Space
                || (vehicle.Faction != null && vehicle.Faction != pawn.Faction)) //Not your vehicle
                return;

            if (pawn.Faction == Faction.OfColony && (pawn.needs.food.CurCategory == HungerCategory.Starving || pawn.needs.rest.CurCategory == RestCategory.Exhausted))
            {
                Messages.Message(pawn.LabelCap + "cannot mount on " + vehicle.LabelCap + ": " + pawn.LabelCap + "is starving or exhausted", MessageSound.RejectInput);
                return;
            }

            if (vehicle.TryGetComp<CompEngine>() != null)
            {
                this.DrivingWorker.pather.StopDead();
                this.DrivingWorker.jobs.StopAll();
            }

            if (this.container.TryAdd(pawn))
            {
                pawn.holder = this.GetContainer();
                pawn.holder.owner = this;
            }
        }

        public void Dismount()
        {
            if (!this.IsMounted)
                return;

            if (vehicle.TryGetComp<CompEngine>() != null)
            {
                this.DrivingWorker.pather.StopDead();
                this.DrivingWorker.jobs.StopAll();
            }

            Thing dummy;
            this.container.TryDrop(Driver, vehicle.MountPos, ThingPlaceMode.Near, out dummy);
        }

        private void ProcessForDoor()
        {
            if (Find.TickManager.TicksGame - tickLastDoorCheck >= TickCooldownDoorCheck
            && (DrivingWorker.Position.GetEdifice() is Building_Door || vehicle.Position.GetEdifice() is Building_Door))
            {
                lastPassedDoor = ((DrivingWorker.Position.GetEdifice() is Building_Door) ?
                    DrivingWorker.Position.GetEdifice() : vehicle.Position.GetEdifice()) as Building_Door;
                lastPassedDoor.StartManualOpenBy(DrivingWorker);
                tickLastDoorCheck = Find.TickManager.TicksGame;
            }
            else if (Find.TickManager.TicksGame - tickLastDoorCheck >= TickCooldownDoorCheck && lastPassedDoor != null)
            {
                lastPassedDoor.StartManualCloseBy(DrivingWorker);
                lastPassedDoor = null;
            }
        }
    }
}
