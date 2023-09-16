using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using SFK.Steamworks.Boiler;
using SFK.Steamworks.SteamEngine;
using SFK.Steamworks.RollingMachine;

[assembly: ModInfo("[SFK] Steamworks", "sfksteamworks",
  Description = "Steampunk. More. Now with machines!",
  Version = "0.1.4",
  Website = "https://github.com/LMD-inc/Steampunkofication",
  Authors = new string[] { "LMD inc." })]

namespace SFK.Steamworks
{
  public class SteamworksMod : ModSystem
  {
    public override void Start(ICoreAPI api)
    {
      base.Start(api);

      // Boiler
      api.RegisterBlockClass("Boiler", typeof(BlockBoiler));
      api.RegisterBlockEntityClass("BEBoiler", typeof(BEBoiler));

      api.RegisterBlockClass("MultiblockBoiler", typeof(BlockMultiblockBoiler));

      // Steam Engine
      api.RegisterBlockClass("SteamEngine", typeof(BlockSteamEngine));
      api.RegisterBlockEntityClass("SteamEngine", typeof(BESteamEngine));
      api.RegisterBlockEntityBehaviorClass("MPSteamEngine", typeof(BEBehaviorMPSteamEngine));

      api.RegisterBlockClass("MultiblockSteamEngine", typeof(BlockMultiblockSteamEngine));

      // Rolling Machine
      api.RegisterBlockClass("RollingMachine", typeof(BlockRollingMachine));
      api.RegisterBlockEntityClass("BERollingMachine", typeof(BERollingMachine));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
