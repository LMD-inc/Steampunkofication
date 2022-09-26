using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using SFK.Steamworks.Boiler;
using SFK.Steamworks.SteamEngine;

[assembly: ModInfo("[SFK] Steamworks",
  Description = "Steampunk. More. Now with machines!",
  Version = "0.1.3",
  Website = "https://github.com/LMD-inc/Steampunkofication",
  Authors = new[] { "LMD inc." })]

namespace SFK.Steamworks
{
  public class SteamworksMod : ModSystem
  {
    public override void Start(ICoreAPI api)
    {
      base.Start(api);

      api.RegisterBlockClass("Boiler", typeof(BlockBoiler));
      api.RegisterBlockEntityClass("BEBoiler", typeof(BEBoiler));

      api.RegisterBlockClass("MultiblockBoiler", typeof(BlockMultiblockBoiler));

      api.RegisterBlockClass("SteamEngine", typeof(BlockSteamEngine));
      api.RegisterBlockEntityClass("SteamEngine", typeof(BESteamEngine));
      api.RegisterBlockEntityBehaviorClass("MPSteamEngine", typeof(BEBehaviorMPSteamEngine));

      api.RegisterBlockClass("MultiblockSteamEngine", typeof(BlockMultiblockSteamEngine));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
