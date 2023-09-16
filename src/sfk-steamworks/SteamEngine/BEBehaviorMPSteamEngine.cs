
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;
using SFK.API;
using System.Text;

namespace SFK.Steamworks.SteamEngine
{
  public class BEBehaviorMPSteamEngine : BEBehaviorMPRotor
  {
    public bool isPowered;

    protected override AssetLocation Sound => null;

    protected override float Resistance => 0.3f;
    protected override double AccelerationFactor => 1d;
    protected override float TargetSpeed => isPowered ? 1f : 0;
    protected override float TorqueFactor => isPowered ? 1.5f : 0;
    public override float AngleRad => 0;
    BlockEntityAnimationUtil animUtil => Blockentity.GetBehavior<BEBehaviorAnimatable>().animUtil;

    public BEBehaviorMPSteamEngine(BlockEntity blockentity) : base(blockentity)
    {
      Blockentity = blockentity;

      string orientation = blockentity.Block.Variant["side"];
      ownFacing = BlockFacing.FromCode(orientation).GetCCW();
      OutFacingForNetworkDiscovery = ownFacing.Opposite;
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
      base.Initialize(api, properties);

      if (api.World.Side == EnumAppSide.Client && animUtil != null)
      {
        float rotY = Block.Shape.rotateY;
        animUtil.InitializeAnimator("sfksteamworks:steamengine", null, null, new Vec3f(0, rotY, 0));
      }

      Blockentity.RegisterGameTickListener(CheckSteamPowered, 1000);
    }

    private void CheckSteamPowered(float dt)
    {
      ItemSlotGasOnly steamSlot = (Blockentity as BEGasContainer).Inventory.FirstOrDefault(slot =>
      {
        return !slot.Empty && slot.Itemstack?.Item?.Code.ToString() == "sfksteamworks:steamportion";
      }) as ItemSlotGasOnly;

      if (steamSlot != null && steamSlot.StackSize > 0)
      {
        isPowered = true;
        steamSlot.TakeOut(1); // Consumption

        if (!animUtil.activeAnimationsByAnimCode.ContainsKey("work"))
        {
          animUtil.StartAnimation(new AnimationMetaData() { Animation = "work", Code = "work", Weight = 10 });
        }
      }
      else
      {
        isPowered = false;
        animUtil.StopAnimation("work");
      }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
      sb.Clear();

      if (isPowered) sb.AppendLine(Lang.Get("Working"));
      else sb.AppendLine(Lang.Get("Stale"));
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
    {
      isPowered = tree.GetBool("p");
      base.FromTreeAttributes(tree, world);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      tree.SetBool("p", isPowered);
      base.ToTreeAttributes(tree);
    }
  }
}
