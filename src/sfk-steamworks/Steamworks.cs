using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using SFK.Steamworks.Boiler;

[assembly: ModInfo("[SFK] Steamworks",
  Description = "Steampunk. More. Now with machines!",
  Version = "0.1.0",
  Website = "https://github.com/LMD-inc/Steampunkofication",
  Authors = new[] { "simplewepro" })]

namespace SFK.Steamworks
{
  public class SteamworksMod : ModSystem
  {
    public override void Start(ICoreAPI api)
    {
      base.Start(api);

      api.RegisterBlockClass("Boiler", typeof(BlockBoiler));
      api.RegisterBlockEntityClass("BEBoiler", typeof(BEBoiler));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
