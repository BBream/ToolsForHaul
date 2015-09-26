using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicle
{
    public class CompWheel : ThingComp
    {

        Graphic_Multi graphic = null;
        double tick_time = 0;

        protected CompWheel() { }

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }
        public override void CompTick()
        {
            tick_time += 0.1;
        }
        //public virtual void PostDeSpawn();
        //public virtual void PostDestroy(DestroyMode mode = DestroyMode.Vanish);
        public override void PostDraw()
        {
            float wheelShake = (float)((Math.Sin(tick_time) + Math.Abs(Math.Sin(tick_time))) / 40.0);
            IntRot rotation = new IntRot(0);
            Vector3 drawLoc = new Vector3(parent.Position.x, parent.Position.y, parent.Position.z);
            Vector3 compOffset = new Vector3((float)-0.2, (float)0, (float)(0.2 + wheelShake));

            graphic.Draw(drawLoc + compOffset, rotation, parent);
        }
        //public virtual void PostExposeData();
        //public virtual void PostPrintOnto(SectionLayer layer);
        public override void PostSpawnSetup()
        {
            graphic = new Graphic_Multi();
            graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cargo_Wheel", parent.def.shader, parent.def.DrawSize, parent.def.defaultColor, parent.def.defaultColorTwo);
        }
    }
}