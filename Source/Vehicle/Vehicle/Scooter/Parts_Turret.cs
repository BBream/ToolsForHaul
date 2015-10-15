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
    public abstract class Parts_Turret : IExposable
    {
        //For working turret, caster should any thing not pawn
        public Thing dummy;
        public Thing parent;
        public Parts_TurretGunDef parts_TurretGunDef;
        private const float SightRadiusTurret = 13.4f;
        protected StunHandler stunner;
        public TargetInfo forcedTarget;

        public abstract TargetInfo CurrentTarget { get; }

        public abstract Verb AttackVerb { get; }

        public Parts_Turret()
        {
            this.forcedTarget = TargetInfo.Invalid;
            this.stunner = new StunHandler((Thing)parent);
        }

        public virtual void Tick()
        {
            this.stunner.StunHandlerTick();
        }

        public virtual void ExposeData()
        {
            Scribe_Deep.LookDeep<Thing>(ref dummy, "dummy");
            Scribe_References.LookReference<Thing>(ref parent, "parent");
            Scribe_Deep.LookDeep<StunHandler>(ref stunner, "stunner", new StunHandler((Thing)parent));
        }

        public virtual void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            dummy.Destroy(mode);
        }

        public virtual void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            this.stunner.Notify_DamageApplied(dinfo, true);
            absorbed = false;
        }

        public abstract void OrderAttack(TargetInfo targ);
    }
}
