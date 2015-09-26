using RimWorld;
using Verse;

namespace Vehicle
{
    public class ThingWithComponentsSpawnPawn : ThingWithComponents
    {
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            var thingDef = (ThingSpawnPawnDef)def;
            var newPawn = PawnGenerator.GeneratePawn(thingDef.spawnPawnDef, Faction.OfColony);
            IntVec3 pos = GenCellFinder.RandomStandableClosewalkCellNear(Position, 2);
            GenSpawn.Spawn(newPawn, pos);

            Destroy();
        }
    }
}
