using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

using SFK.Transportation.Pipe;
using SFK.Transportation.WaterPump;
using SFK.Transportation.Drum;

[assembly: ModInfo("[SFK] Transportation",
  Description = "Steampunk. More. Now with pipes!",
  Version = "0.1.3",
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

      api.RegisterBlockClass("Drum", typeof(BlockDrum));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
