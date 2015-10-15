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
    public class Parts_TurretGun : Parts_Turret
    {
        public Thing gun;
        protected TurretTop top;
        //protected CompPowerTrader powerComp;
        //protected CompMannable mannableComp;
        public bool loaded;
        protected TargetInfo currentTargetInt;
        protected int burstWarmupTicksLeft;
        protected int burstCooldownTicksLeft;

        public CompEquippable GunCompEq
        {
            get
            {
                return ThingCompUtility.TryGetComp<CompEquippable>(this.gun);
            }
        }

        public override TargetInfo CurrentTarget
        {
            get
            {
                return this.currentTargetInt;
            }
        }

        private bool WarmingUp
        {
            get
            {
                return this.burstWarmupTicksLeft > 0;
            }
        }

        public override Verb AttackVerb
        {
            get
            {
                if (this.gun == null)
                    return (Verb) null;
                return this.GunCompEq.verbTracker.PrimaryVerb;
            }
        }

        public Parts_TurretGun()
        {
            this.currentTargetInt = TargetInfo.Invalid;
        }

        public virtual void SpawnSetup()
        {
            dummy = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Parts"), (ThingDef)null);
            IntVec3 PosInt = parent.Position + parts_TurretGunDef.partsOffset.RotatedBy(parent.Rotation.AsAngle).ToIntVec3();
            dummy.Position = parent.Position;
            if (PosInt.InBounds())
                dummy.Position = PosInt;
            dummy.SetFactionDirect(parent.Faction);
            this.gun = ThingMaker.MakeThing(parts_TurretGunDef.turretGunDef, (ThingDef)null);
            this.GunCompEq.verbTracker.InitVerbs();
            for (int index = 0; index < this.GunCompEq.AllVerbs.Count; ++index)
            {
                Verb verb = this.GunCompEq.AllVerbs[index];
                verb.caster = (Thing) dummy;
                verb.castCompleteCallback = () => {BurstComplete();};
            }
            this.top = new TurretTop((Parts_Turret)this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.LookValue<bool>(ref this.loaded, "loaded", false, false);
        }

        public override void OrderAttack(TargetInfo targ)
        {
            if ((double)(targ.Cell - parent.Position).LengthHorizontal < (double)this.GunCompEq.PrimaryVerb.verbProps.minRange)
                Messages.Message(parts_TurretGunDef.turretGunDef.LabelCap + ": " + Translator.Translate("MessageTargetBelowMinimumRange"), MessageSound.RejectInput);
            else if ((double)(targ.Cell - parent.Position).LengthHorizontal > (double)this.GunCompEq.PrimaryVerb.verbProps.range)
                Messages.Message(parts_TurretGunDef.turretGunDef.LabelCap + ": " + Translator.Translate("MessageTargetBeyondMaximumRange"), MessageSound.RejectInput);
            else
                this.forcedTarget = targ;
        }

        public override void Tick()
        {
            base.Tick();
            IntVec3 PosInt = parent.Position + parts_TurretGunDef.partsOffset.RotatedBy(parent.Rotation.AsAngle).ToIntVec3();
            dummy.Position = parent.Position;
            if (PosInt.InBounds())
                dummy.Position = PosInt;
            dummy.SetFactionDirect(parent.Faction);
            if (parent is Vehicle && !(parent as Vehicle).IsMounted)
                return;
            this.GunCompEq.verbTracker.VerbsTick();
            if (this.stunner.Stunned || this.GunCompEq.PrimaryVerb.state == VerbState.Bursting)
                return;
            if (this.WarmingUp)
            {
                --this.burstWarmupTicksLeft;
                if (this.burstWarmupTicksLeft == 0)
                    this.BeginBurst();
            }
            else
            {
                if (this.burstCooldownTicksLeft > 0)
                    --this.burstCooldownTicksLeft;
                if (this.burstCooldownTicksLeft == 0)
                    this.TryStartShootSomething();
            }
            this.top.TurretTopTick();
        }

        protected void TryStartShootSomething()
        {
            if (this.forcedTarget.ThingDestroyed)
                this.forcedTarget = (TargetInfo) ((Thing) null);
            if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && Find.RoofGrid.Roofed(parent.Position))
                return;
            bool isValid = this.currentTargetInt.IsValid;
            this.currentTargetInt = !this.forcedTarget.IsValid ? this.TryFindNewTarget() : this.forcedTarget;
            if (!isValid && this.currentTargetInt.IsValid)
                SoundStarter.PlayOneShot(SoundDefOf.TurretAcquireTarget, (SoundInfo)parent.Position);
            if (!this.currentTargetInt.IsValid)
                return;
            if (parts_TurretGunDef.turretBurstWarmupTicks > 0)
                this.burstWarmupTicksLeft = parts_TurretGunDef.turretBurstWarmupTicks;
            else
                this.BeginBurst();
        }

        protected TargetInfo TryFindNewTarget()
        {
            Thing searcher;
            Faction faction;

            searcher = (Thing)parent;
            faction = parent.Faction;
            if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && FactionUtility.HostileTo(faction, Faction.OfColony) && ((double) Rand.Value < 0.5 && Find.ListerBuildings.allBuildingsColonist.Count > 0))
                return (TargetInfo) ((Thing) GenCollection.RandomElement<Building>((IEnumerable<Building>) Find.ListerBuildings.allBuildingsColonist));
            TargetScanFlags flags = TargetScanFlags.None;
            if (!this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
                flags |= TargetScanFlags.NeedLOSToAll;
            if (!this.GunCompEq.PrimaryVerb.verbProps.ai_IsIncendiary)
                flags |= TargetScanFlags.NeedNonBurning;
            // ISSUE: method pointer
            return (TargetInfo) AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, t => IsValidTarget(t), this.GunCompEq.PrimaryVerb.verbProps.range, this.GunCompEq.PrimaryVerb.verbProps.minRange, flags);
        }

        private bool IsValidTarget(Thing t)
        {
            Pawn p = t as Pawn;
            if (p == null)
                return true;
            if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
            {
                RoofDef roofDef = Find.RoofGrid.RoofAt(t.Position);
                if (roofDef != null && roofDef.isThickRoof)
                    return false;
            }
            return !GenAI.MachinesLike(parent.Faction, p);
        }

        protected void BeginBurst()
        {
            this.GunCompEq.PrimaryVerb.TryStartCastOn(this.CurrentTarget);
        }

        protected void BurstComplete()
        {
            this.burstCooldownTicksLeft = parts_TurretGunDef.turretBurstCooldownTicks < 0 ? this.GunCompEq.PrimaryVerb.verbProps.defaultCooldownTicks : parts_TurretGunDef.turretBurstCooldownTicks;
            this.loaded = false;
        }

        public virtual string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = null;
            if (!GenText.NullOrEmpty(inspectString))
                stringBuilder.AppendLine(inspectString);
            stringBuilder.AppendLine(Translator.Translate("GunInstalled") + ": " + this.gun.LabelCap);
            if ((double) this.GunCompEq.PrimaryVerb.verbProps.minRange > 0.0)
                stringBuilder.AppendLine(Translator.Translate("MinimumRange") + ": " + this.GunCompEq.PrimaryVerb.verbProps.minRange.ToString("F0"));
            if (this.burstCooldownTicksLeft > 0)
                stringBuilder.AppendLine(Translator.Translate("CanFireIn") + ": " + GenDate.TickstoSecondsString(this.burstCooldownTicksLeft));
            if (parts_TurretGunDef.turretShellDef != null)
            {
                if (this.loaded)
                    stringBuilder.AppendLine(Translator.Translate("ShellLoaded"));
                else
                    stringBuilder.AppendLine(Translator.Translate("ShellNotLoaded"));
            }
            return stringBuilder.ToString();
        }

        public virtual void DrawAt(Vector3 drawLoc)
        {
            this.top.DrawTurretAt(drawLoc);
        }

        public virtual void DrawExtraSelectionOverlays()
        {
            float radius1 = this.GunCompEq.PrimaryVerb.verbProps.range;
            if ((double) radius1 < 90.0)
                GenDraw.DrawRadiusRing(parent.Position, radius1);
            float radius2 = this.GunCompEq.PrimaryVerb.verbProps.minRange;
            if ((double) radius2 < 90.0 && (double) radius2 > 0.100000001490116)
                GenDraw.DrawRadiusRing(parent.Position, radius2);
            if (this.burstWarmupTicksLeft <= 0)
                return;
            GenDraw.DrawAimPie((Thing)parent, this.CurrentTarget, (int)((double)this.burstWarmupTicksLeft * 0.5), (float)parent.def.size.x * 0.5f);
        }
    }
}
