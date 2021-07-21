using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

using SFK.Transportation.Pipe;
using SFK.Transportation.WaterPump;

[assembly: ModInfo("[SFK] Transportation",
  Description = "Steampunk. More. Soon with pipes included!",
  Version = "0.1.0",
  Website = "https://github.com/LMD-inc/Steampunkofication",
  Authors = new[] { "LMD inc." })]

namespace SFK.Transportation
{
  public class TransportationMod : ModSystem
  {
    public override void Start(ICoreAPI api)
    {
      base.Start(api);

      api.RegisterBlockClass("GasPipe", typeof(BlockGasPipe));
      api.RegisterBlockEntityClass("GasPipe", typeof(BlockEntityGasPipe));

      api.RegisterBlockClass("LiquidPipe", typeof(BlockLiquidPipe));
      api.RegisterBlockEntityClass("LiquidPipe", typeof(BlockEntityLiquidPipe));

      api.RegisterBlockClass("WaterPump", typeof(BlockWaterPump));
      api.RegisterBlockEntityClass("WaterPump", typeof(BEWaterPump));
      api.RegisterBlockEntityBehaviorClass("WaterPump", typeof(BEBehaviorWaterPump));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
