using System.Collections.Generic;
using Verse;

namespace Vehicle
{
	public class ThingSpawnPawnDef : ThingDef
	{
        /// <summary>
        /// What to spawn.
        /// </summary>
        public PawnKindDef spawnPawnDef;
        /// <summary>
        /// storage config - not implemented
        /// </summary>
        //public int maxStackSpace = 150;
	}
}
