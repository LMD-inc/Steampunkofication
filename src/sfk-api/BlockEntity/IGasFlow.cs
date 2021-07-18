using Vintagestory.API.MathTools;

namespace SFK.API
{
  public interface IGasFLow
  {
    BlockFacing[] GasPullFaces { get; set; }
    BlockFacing[] GasPushFaces { get; set; }
    BlockFacing[] AcceptGasFromFaces { get; set; }
  }
}
