using Vintagestory.API.MathTools;

namespace SFK.API
{
  public interface ILiquidFLow
  {
    BlockFacing[] LiquidPullFaces { get; set; }
    BlockFacing[] LiquidPushFaces { get; set; }
    BlockFacing[] AcceptLiquidFromFaces { get; set; }
  }
}
