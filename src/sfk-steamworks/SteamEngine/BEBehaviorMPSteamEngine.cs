
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;
using SFK.API;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace SFK.Steamworks.SteamEngine
{
  public class BEBehaviorMPSteamEngine : BEBehaviorMPRotor
  {
    ICoreClientAPI capi;

    public bool isPowered;

    protected override AssetLocation Sound => null;

    protected override float Resistance => 0.3f;
    protected override double AccelerationFactor => 1d;
    protected override float TargetSpeed => isPowered ? 1f : 0;
    protected override float TorqueFactor => isPowered ? 1.5f : 0;
    BlockEntityAnimationUtil animUtil
    {
      get { return Blockentity.GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
    }

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

      switch (ownFacing.Code)
      {
        case "north":
        case "south":
          AxisSign = new int[] { 0, 0, -1 };
          break;

        case "east":
        case "west":
          AxisSign = new int[] { -1, 0, 0 };
          break;
      }

      if (api.World.Side == EnumAppSide.Client)
      {
        capi = api as ICoreClientAPI;

        if (animUtil != null)
        {
          float rotY = Block.Shape.rotateY;
          Shape shape = capi.Assets.TryGet("sfksteamworks:shapes/block/machine/steamengine/base.json").ToObject<Shape>();
          animUtil.InitializeAnimator("sfksteamworks:steamengine", shape, null, new Vec3f(0, rotY, 0));
        }
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

        animUtil?.StartAnimation(new AnimationMetaData() { Animation = "work", Code = "work", Weight = 10 });
      }
      else
      {
        isPowered = false;
        animUtil?.StopAnimation("work");
      }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
      sb.Clear();

      if (isPowered) sb.AppendLine(Lang.Get("sfksteamworks:Working"));
      else sb.AppendLine(Lang.Get("sfksteamworks:Stale"));
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

    #region Tesselation
    MeshData GetBaseMesh(string orient)
    {
      return ObjectCacheUtil.GetOrCreate(Api, "steamengine-" + orient + "-base", () =>
      {
        Shape shape = capi.Assets.TryGet("sfksteamworks:shapes/block/machine/steamengine/base.json").ToObject<Shape>();
        MeshData mesh;
        capi.Tesselator.TesselateShape(Block, shape, out mesh);

        int angleIdx = BlockFacing.FromCode(orient).HorizontalAngleIndex;

        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, (90 * angleIdx * GameMath.PI) / 180, 0);

        return mesh;
      });
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
      if (!isPowered)
      {
        mesher.AddMeshData(GetBaseMesh(Block.Variant["side"]));
      }

      return base.OnTesselation(mesher, tesselator);
    }

    #endregion
  }
}
