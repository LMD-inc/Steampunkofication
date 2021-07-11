using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

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
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    public override void StartServerSide(ICoreServerAPI api)
    {

    }
  }
}
