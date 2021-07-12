using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace SFK.API
{
  public interface IBEMPMultiblock : IBlockEntityContainer
  {
    BlockPos Principal { get; set; }

    void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world);
    void GetBlockInfo(IPlayer forPlayer, StringBuilder sb);
    void Initialize(ICoreAPI api);
    void OnBlockPlaced(ItemStack byItemStack = null);
    void ToTreeAttributes(ITreeAttribute tree);
  }
}