using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
  public class Designator_InstallForBackpack : Designator_Place
  {
    private static readonly Texture2D InstallTexture;
    public Thing miniThing;
    public Pawn wearer;

    private MinifiedThing MiniThing
    {
      get
      {
          return (MinifiedThing)miniThing;
      }
    }

    public override BuildableDef PlacingDef
    {
      get
      {
          return (BuildableDef)this.MiniThing.InnerThing.def;
      }
    }

    public override string Label
    {
      get
      {
        return Translator.Translate("CommandInstall");
      }
    }

    public override string Desc
    {
      get
      {
        return Translator.Translate("CommandInstallDesc");
      }
    }

    protected override Color IconDrawColor
    {
      get
      {
        return Color.white;
      }
    }

    public override bool Visible
    {
      get
      {
        if (miniThing == null)
          return false;
        return base.Visible;
      }
    }

    static Designator_InstallForBackpack()
    {
      Designator_InstallForBackpack.InstallTexture = ContentFinder<Texture2D>.Get("UI/Commands/Install", true);
    }

    public Designator_InstallForBackpack()
    {
      this.icon = Designator_InstallForBackpack.InstallTexture;
      //this.iconOverdraw = false;
    }

    public override bool CanRemainSelected()
    {
        return miniThing is MinifiedThing;
    }

    public override void ProcessInput(Event ev)
    {
        MinifiedThing minifiedThing = miniThing as MinifiedThing;
      if (minifiedThing != null)
        minifiedThing.CancelExistingBlueprints();
      base.ProcessInput(ev);
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
      return GenConstruct.CanPlaceBlueprintAt(this.PlacingDef, c, this.placingRot, false);
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        Thing dummy;
        MiniThing.holder.TryDrop(MiniThing, MiniThing.PositionHeld, ThingPlaceMode.Near, out dummy);
        GenSpawn.WipeExistingThings(c, this.placingRot, (BuildableDef)this.PlacingDef.blueprintDef, true);
        GenConstruct.PlaceBlueprintForInstall(this.MiniThing, c, this.placingRot, Faction.OfColony);
        MoteThrower.ThrowMetaPuffs(GenAdj.OccupiedRect(c, this.placingRot, this.PlacingDef.Size));
        DesignatorManager.Deselect();
    }

    protected override void DrawGhost(Color ghostCol)
    {
      GhostDrawer.DrawGhostThing(Gen.MouseCell(), this.placingRot, (ThingDef) this.PlacingDef, GraphicUtility.ExtractInnerGraphicFor(this.MiniThing.InnerThing.Graphic, this.MiniThing.InnerThing), ghostCol, AltitudeLayer.Blueprint);
    }
  }
}
