using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace SFK.Steamworks.RollingMachine
{
  public enum EnumRollingMachineSection
  {
    Base,
    Handle
  }

  public class BERollingMachine : BlockEntityContainer, ITexPositionSource
  {
    InventoryRollingMachine inv;
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => "rollingmachine";

    public bool HasRollers => !inv[0].Empty;

    private bool isRolling;
    public bool IsRolling
    {
      get
      {
        return isRolling;
      }
      set
      {
        isRolling = value;

        if (renderer != null)
        {
          renderer.IsRolling = isRolling;
          renderer.ShouldRender = isRolling;
          UpdateRollingState();
        }
      }
    }

    public bool CanRoll => HasRollers;

    #region Render

    public BlockFacing Facing { get; protected set; } = BlockFacing.NORTH;
    float RotateY = 0f;

    public string RollersMaterial
    {
      get
      {
        if (!HasRollers) return null;
        return inv[0].Itemstack.Collectible.Variant["metal"];
      }
    }
    MeshData RollingMachineStandMesh
    {
      get
      {
        object value;
        Api.ObjectCache.TryGetValue("rollingmachinestandmesh", out value);
        return (MeshData)value;
      }
      set
      {
        Api.ObjectCache["rollingmachinestandmesh"] = value;
      }
    }

    MeshData RollerMesh;

    MeshData HandleMesh;

    RollingMachineRenderer renderer;

    ITexPositionSource tmpTextureSource;

    public TextureAtlasPosition this[string textureCode]
    {
      get { return tmpTextureSource["roller-" + RollersMaterial]; }
    }

    public Size2i AtlasSize
    {
      get { return ((ICoreClientAPI)Api).BlockTextureAtlas.Size; }
    }

    TextureAtlasPosition rollertexpos;

    #endregion

    public BERollingMachine()
    {
      inv = new InventoryRollingMachine(this, 2);
    }

    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      inv.LateInitialize(InventoryClassName + "-" + Pos, api);

      Facing = BlockFacing.FromCode(Block.Variant["side"]);
      if (Facing == null) Facing = BlockFacing.NORTH;

      switch (Facing.Index)
      {
        case 1:
          RotateY = 270;
          break;
        case 2:
          RotateY = 180;
          break;
        case 3:
          RotateY = 90;
          break;
        default:
          break;
      }

      if (api is ICoreServerAPI sapi)
      {
        RegisterGameTickListener(OnServerTick, 200);
      }

      if (api is ICoreClientAPI capi)
      {
        renderer = new RollingMachineRenderer(capi, Pos, GenMesh("handle"), RotateY);

        capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "rollingmachine");

        if (RollingMachineStandMesh == null)
        {
          RollingMachineStandMesh = GenMesh("stand");
        }

        if (HandleMesh == null)
        {
          HandleMesh = GenMesh("handle");
        }

        if (HasRollers && RollerMesh == null)
        {
          RollerMesh = GenRollerMesh(capi);
          renderer.UpdateRollersMeshes(RollerMesh, rollertexpos);
        }
      }
    }

    #region Events

    private void OnServerTick(float dt)
    {
      if (!HasRollers) return;

      if (!inv[1].Empty)
      {

      }
    }

    internal bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumRollingMachineSection section)
    {
      if (section == EnumRollingMachineSection.Base && byPlayer.Entity.Controls.ShiftKey)
      {
        return TryPut(world, byPlayer, blockSel);
      }

      if (section == EnumRollingMachineSection.Handle && CanRoll)
      {
        IsRolling = true;
        return true;
      }

      return false;
    }

    internal bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      IsRolling = true;
      return CanRoll;
    }

    internal void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      IsRolling = false;
    }

    private void OnRetesselated()
    {
      if (renderer == null) return; // Maybe already disposed

      renderer.ShouldRender = HasRollers && IsRolling;
    }

    #endregion

    private bool TryPut(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
      ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
      if (slot.Itemstack == null) return false;
      ItemStack stack = slot.Itemstack;

      // rollers
      if (!HasRollers)
      {
        if (stack.Collectible.FirstCodePart() == "roller")
        {
          if (stack.StackSize < 2)
          {
            (Api as ICoreClientAPI)?.TriggerIngameError(this, "require2rollers", Lang.Get("Please add 2 rollers at the same time!"));
            return true;
          }

          inv[0].Itemstack = slot.TakeOut(2);

          if (Api is ICoreClientAPI capi && RollerMesh == null)
          {
            RollerMesh = GenRollerMesh(capi);
            renderer.UpdateRollersMeshes(RollerMesh, rollertexpos);
          }

          slot.MarkDirty();
          MarkDirty(true);
        }

        return false;
      }

      // work item
      if (stack.Collectible is IAnvilWorkable workableobj)
      {
        int requiredTier = workableobj.GetRequiredAnvilTier(stack);
        if (requiredTier > RollersTier)
        {
          if (world.Side == EnumAppSide.Client)
          {
            (Api as ICoreClientAPI).TriggerIngameError(this, "toolowtier", Lang.Get("Working this metal needs a tier {0} rollers", requiredTier));
          }

          return false;
        }
      }

      return false;
    }

    private int RollersTier
    {
      get
      {
        if (!HasRollers) return 0;

        MetalPropertyVariant var;

        if (Api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(RollersMaterial, out var))
        {
          return var.Tier;
        }

        return 0;
      }
    }

    internal MeshData GenMesh(string type)
    {
      Block block = Api.World.BlockAccessor.GetBlock(Pos);
      if (block.BlockId == 0) return null;

      ICoreClientAPI capi = Api as ICoreClientAPI;
      ITesselatorAPI mesher = capi.Tesselator;

      return ObjectCacheUtil.GetOrCreate(capi, type, () =>
      {
        MeshData mesh;
        mesher.TesselateShape(block, Shape.TryGet(capi, $"sfksteamworks:shapes/block/machine/rollingmachine/{type}.json"), out mesh);
        return mesh;
      });
    }

    internal MeshData GenRollerMesh(ICoreClientAPI capi)
    {
      if (!HasRollers) return null;

      Block tmpblock = capi.World.BlockAccessor.GetBlock(Pos);
      Item rollerItem = inv[0].Itemstack.Item;
      tmpTextureSource = capi.Tesselator.GetTexSource(tmpblock);
      rollertexpos = capi.BlockTextureAtlas.GetPosition(tmpblock, "metal");

      return ObjectCacheUtil.GetOrCreate(capi, $"roller-{RollersMaterial}", () =>
      {
        MeshData rollerMesh;
        capi.Tesselator.TesselateItem(rollerItem, out rollerMesh, this);
        return rollerMesh;
      });
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
      if (Block == null) return false;

      mesher.AddMeshData(
        this.RollingMachineStandMesh.Clone()
        .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
      );

      // when not in action - apply static mesh tesselation renderer
      if (!isRolling)
      {
        Vec3f Rot = GetRotation(renderer.AngleRad);

        mesher.AddMeshData(
          this.HandleMesh.Clone()
          .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
          .Rotate(renderer.handleOrig, Rot.X, Rot.Y, -Rot.Z)
        );

        if (HasRollers)
        {
          mesher.AddMeshData(
              this.RollerMesh.Clone()
              .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
              .Rotate(renderer.rollerOrig, Rot.X, Rot.Y, -Rot.Z)
              .Translate(renderer.topRollerPos)
          );

          mesher.AddMeshData(
              this.RollerMesh.Clone()
              .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, RotateY * GameMath.DEG2RAD, 0)
              .Rotate(renderer.rollerOrig, Rot.X, Rot.Y, Rot.Z)
              .Translate(renderer.downRollerPos)
          );
        }
      }

      return true;
    }

    Vec3f GetRotation(float AngleRad)
    {
      switch (RotateY)
      {
        default:
        case 0:
          return new Vec3f(AngleRad, 0, 0);
        case 90:
          return new Vec3f(0, 0, AngleRad);
        case 180:
          return new Vec3f(-AngleRad, 0, 0);
        case 270:
          return new Vec3f(0, 0, -AngleRad);
      }
    }

    bool prevIsRolling;
    void UpdateRollingState()
    {
      if (Api?.World == null) return;

      if (prevIsRolling != isRolling)
      {
        Api.World.BlockAccessor.MarkBlockDirty(Pos, OnRetesselated);

        if (isRolling)
        {
          // ambientSound?.Start();
        }
        else
        {
          // ambientSound?.Stop();
        }

        if (Api.Side == EnumAppSide.Server)
        {
          MarkDirty();
        }
      }

      prevIsRolling = isRolling;
    }

    #region TreeAttributes

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
      base.FromTreeAttributes(tree, worldForResolving);
      Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

      if (Api != null)
      {
        Inventory.AfterBlocksLoaded(Api.World);
      }

      if (worldForResolving.Side == EnumAppSide.Client)
      {
        UpdateRollingState();
      }
    }

    #endregion
  }
}
