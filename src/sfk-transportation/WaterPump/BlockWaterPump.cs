using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.WaterPump
{
  public class BlockWaterPump : BlockMPBase
  {
    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
      return face == BlockFacing.FromCode(Variant["side"]).GetCW();
    }
  }
}
