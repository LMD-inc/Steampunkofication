using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SFK.Transportation.Belt
{
  public class BlockBeltWithAxle : BlockBelt
  {
    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      // for ns and ew just take by first letter
      BlockFacing orient = BlockFacing.FromFirstLetter(Variant["orient"].ElementAt(0)).GetCW();
      if (face == orient || face == orient.Opposite) return true;

      return base.HasMechPowerConnectorAt(world, pos, face);
    }
  }
}
