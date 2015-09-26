using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

using Verse;
using Verse.AI;
using RimWorld;


namespace ToolsForHaul
{
    public class Verb_PutIntoInventory : Verb
    {
        /*
        protected int burstShotsLeft;
        public Action castCompleteCallback;
        public Thing caster;
        protected TargetInfo currentTarget;
        public ThingWithComps ownerEquipment;
        public HediffComp_VerbGiver ownerHediffComp;
        public VerbState state;
        protected int ticksToNextBurstShot;
        public VerbProperties verbProps;

        protected Verb();

        public bool CasterIsPawn { get; }
        public Pawn CasterPawn { get; }
        protected virtual int ShotsPerBurst { get; }
        public virtual Texture2D UIIcon { get; }

        public bool CanHitTarget(TargetInfo targ);
        public virtual bool CanHitTargetFrom(IntVec3 root, TargetInfo targ);
        public float GetDamageFactorFor(Pawn pawn);
        public virtual float HighlightFieldRadiusAroundTarget();
        protected virtual void InitCast();
        public bool IsStillUsableBy(Pawn pawn);
        public void Notify_PickedUp();
        public override string ToString();
        protected void TryCastNextBurstShot();
        protected abstract bool TryCastShot();
        public bool TryFindShootLineFromTo(IntVec3 root, TargetInfo targ, out ShootLine resultingLine);
        public bool TryStartCastOn(TargetInfo castTarg);
        public void VerbTick();
         * */

        protected override bool TryCastShot()
        {
            Pawn casterPawn = this.CasterPawn;
            Thing thing1 = currentTarget.Thing;

            if (!this.CanHitTarget((TargetInfo)thing1))
            {
                object[] objArray = new object[4];
                int index1 = 0;
                Pawn pawn = casterPawn;
                objArray[index1] = (object)pawn;
                int index2 = 1;
                string str1 = " put in ";
                objArray[index2] = (object)str1;
                int index3 = 2;
                Thing thing2 = thing1;
                objArray[index3] = (object)thing2;
                int index4 = 3;
                string str2 = " from out of position.";
                objArray[index4] = (object)str2;
                Log.Warning(string.Concat(objArray));
            }
            casterPawn.drawer.rotator.FaceCell(thing1.Position);

            if (CasterIsPawn)
            {
                casterPawn = CasterPawn;
                casterPawn.inventory.container.TryAdd(thing1);
                return true;
            }

            return false;
        }
    }

}