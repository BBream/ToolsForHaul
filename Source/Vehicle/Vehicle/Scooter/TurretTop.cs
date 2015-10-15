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
  public class TurretTop
  {
    private const float IdleTurnDegreesPerTick = 0.26f;
    private const int IdleTurnDuration = 140;
    private const int IdleTurnIntervalMin = 150;
    private const int IdleTurnIntervalMax = 350;
    private Parts_Turret parentTurret;
    private float curRotationInt;
    private int ticksUntilIdleTurn;
    private int idleTurnTicksLeft;
    private bool idleTurnClockwise;

    private float CurRotation
    {
      get
      {
        return this.curRotationInt;
      }
      set
      {
        this.curRotationInt = value;
        if ((double) this.curRotationInt > 360.0)
          this.curRotationInt -= 360f;
        if ((double) this.curRotationInt >= 0.0)
          return;
        this.curRotationInt += 360f;
      }
    }

    public TurretTop(Parts_Turret ParentTurret)
    {
      this.parentTurret = ParentTurret;
    }

    public void TurretTopTick()
    {
      TargetInfo currentTarget = this.parentTurret.CurrentTarget;
      if (currentTarget.IsValid)
      {
          this.CurRotation = Vector3Utility.AngleFlat(currentTarget.Cell.ToVector3Shifted() - (this.parentTurret.parent.DrawPos + this.parentTurret.parts_TurretGunDef.partsOffset));
        this.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
      }
      else if (this.ticksUntilIdleTurn > 0)
      {
        --this.ticksUntilIdleTurn;
        if (this.ticksUntilIdleTurn != 0)
          return;
        this.idleTurnClockwise = (double) Rand.Value < 0.5;
        this.idleTurnTicksLeft = 140;
      }
      else
      {
        if (this.idleTurnClockwise)
          this.CurRotation += 0.26f;
        else
          this.CurRotation -= 0.26f;
        --this.idleTurnTicksLeft;
        if (this.idleTurnTicksLeft > 0)
          return;
        this.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
      }
    }

    public void DrawTurretAt(Vector3 drawLoc)
    {
      Matrix4x4 matrix = new Matrix4x4();
      matrix.SetTRS(drawLoc, Gen.ToQuat(this.CurRotation), Vector3.one);
      Graphics.DrawMesh(MeshPool.plane20, matrix, MaterialPool.MatFrom(this.parentTurret.parts_TurretGunDef.turretTopExtraGraphicData.graphicPath), 0);
    }
  }
}