using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Steampunkofication.src.Boiler;

[assembly: ModInfo("Steampunkofication",
  Description = "Steampunk. More.",
  Website = "https://github.com/LMD-inc/Steampunkofication",
  Authors = new[] { "simplewepro" })]

namespace Steampunkofication
{
  public class SteampunkoficationMod : ModSystem
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
