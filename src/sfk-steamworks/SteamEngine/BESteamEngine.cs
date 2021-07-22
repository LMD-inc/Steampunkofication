using System.Text;
using Vintagestory.API.Common;
using SFK.API;

namespace SFK.Steamworks.SteamEngine
{
  public class BESteamEngine : BEGasContainer
  {
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
      dsc.Clear();

      GetBehavior<BEBehaviorMPSteamEngine>()?.GetBlockInfo(forPlayer, dsc);
    }
  }
}