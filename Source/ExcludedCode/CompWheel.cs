using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicle
{
    public class CompAttachCargo : CompAttachBase
    {
        CargoWheel cargoWheel = null;

        public void AddWheel(CargoWheel wheel)
        {
            //cargoWheel = wheel;
        }

        public override void PostSpawnSetup()
        {
            //cargoWheel.SpawnSetup();

        }

        public override void CompTick()
        {
            //cargoWheel.Tick();
        }

        public override void PostDraw()
        {
            //cargoWheel.DrawAt(new Vector3(parent.Position.x, parent.Position.y, parent.Position.z));            
        }
        //public override void PostDeSpawn();
        //public override void PostDestroy(DestroyMode mode = DestroyMode.Vanish);
    }

    public class CargoWheel : AttachableThing
    {
        Graphic_Multi graphic = null;
        double tick_time = 0;

        public override string InspectStringAddon { get { return "Wheel is attached"; } }

        public override Vector3 DrawPos { get { return new Vector3(parent.Position.x, parent.Position.y, parent.Position.z); } }

        public override void AttachTo(Thing parent)
        {
            Log.Message("Class CargoWheel indicator");
            SpawnSetup();
            GenSpawn.Spawn(this, parent.Position);
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            graphic = new Graphic_Multi();
            graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo_Wheel", parent.def.shader, parent.def.DrawSize, parent.def.defaultColor, parent.def.defaultColorTwo);
        }

        public override void Tick()
        {
 	        base.Tick();
            tick_time += 0.1;
            //DrawAt(new Vector3(parent.Position.x, parent.Position.y, parent.Position.z));
        }

        public override Graphic Graphic
        {
	        get 
	        {
                return graphic;
	        }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
 	        base.DrawAt(drawLoc);
            float wheelShake = (float)((Math.Sin(tick_time) + Math.Abs(Math.Sin(tick_time))) / 40.0);
            IntRot rotation = new IntRot(0);
            Vector3 compOffset = new Vector3((float)-1.2, (float)0, (float)(1.2 + wheelShake));

            Log.Message("Wheel graphic info" + graphic.GraphicPath + graphic.MatSide.name);
            graphic.Draw(drawLoc + compOffset, rotation, parent);
        }

    }

    /*public class CargoHandle : AttachableThing
    {
        public Thing parent;

        protected AttachableThing();

        public override Vector3 DrawPos { get; }
        public abstract string InspectStringAddon { get; }

        public virtual void AttachTo(Thing parent);
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish);
        public override void ExposeData();
    }*/
}