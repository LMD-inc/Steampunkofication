using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SFK.Transportation.Drum
{
  public class BEDrum : BlockEntityLiquidContainer
  {
    public override string InventoryClassName => "drum";

    public BEDrum()
    {
      inventory = new InventoryGeneric(1, null, null);
    }

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);
    }
  }
}
