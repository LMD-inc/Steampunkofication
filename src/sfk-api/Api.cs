using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo("[SFK] Api",
  Description = "Steampunk. More. It's based.",
  Version = "0.1.2",
  Website = "https://github.com/LMD-inc/Steampunkofication",
  Authors = new[] { "LMD inc." })]

namespace SFK.API
{
  public class SFKApi : ModSystem
  {
    public override void Start(ICoreAPI api)
    {
      base.Start(api);

      api.RegisterItemClass("ItemGasPortion", typeof(ItemGasPortion));

      api.RegisterBlockEntityClass("GasFlow", typeof(BlockEntityGasFlow));
      api.RegisterBlockEntityClass("MultiblockGasFlow", typeof(BEMultiblockGasFlow));
      api.RegisterBlockEntityClass("GasContainer", typeof(BEGasContainer));

      api.RegisterBlockEntityClass("LiquidFlow", typeof(BlockEntityLiquidFlow));
      api.RegisterBlockEntityClass("LiquidContainer", typeof(BELiquidContainer));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
