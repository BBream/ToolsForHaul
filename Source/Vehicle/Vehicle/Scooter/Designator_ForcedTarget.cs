using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace Vehicle
{
    public class Designator_ForcedTarget : Designator
    {
        private const string txtCannotSetForcedTarget = "CannotSetForcedTarget";

        public List<Parts_TurretGun> turretGuns;

        public Designator_ForcedTarget()
            : base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 1; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            foreach (Thing thing in loc.GetThingList())
            {
                Pawn p = thing as Pawn;
                if (p == null || !GenAI.MachinesLike(turretGuns.First().parent.Faction, p))
                    return true;
            }
            return new AcceptanceReport(txtCannotSetForcedTarget.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            foreach (Thing thing in c.GetThingList())
            {
                Pawn p = thing as Pawn;
                if (p == null || !GenAI.MachinesLike(turretGuns.First().parent.Faction, p))
                {
                    foreach (Parts_TurretGun turretGun in turretGuns)
                        turretGun.OrderAttack(thing);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}