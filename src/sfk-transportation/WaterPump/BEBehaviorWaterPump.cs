using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.WaterPump
{
  public class BEBehaviorWaterPump : BEBehaviorMPBase
  {
    ICoreClientAPI capi;
    protected BlockFacing ownFacing;

    public BEBehaviorWaterPump(BlockEntity blockentity) : base(blockentity)
    {
      Blockentity = blockentity;

      string orientation = blockentity.Block.Variant["side"];
      ownFacing = BlockFacing.FromCode(orientation);
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

      if (api.Side == EnumAppSide.Client)
      {
        capi = api as ICoreClientAPI;
      }
    }

    public override float GetResistance()
    {
      //Exponentially increase hammer resistance if the network is turning faster - should almost always prevent helvehammering at crazy speeds;
      float speed = this.network == null ? 0f : Math.Abs(this.network.Speed * this.GearedRatio);
      float speedLimiter = 5f * (float)Math.Exp(speed * 2.8 - 5.0);
      return 0.125f + speedLimiter;
    }

    public override void JoinNetwork(MechanicalNetwork network)
    {
      base.JoinNetwork(network);

      //Speed limit when joining a toggle to an existing network: this is to prevent crazy bursts of waterpump speed on first connection if the network was spinning fast (with low resistances)
      // (if the network has enough torque to drive faster than this - which is going to be uncommon - then the network speed can increase after the toggle is joined to the network)
      float speed = network == null ? 0f : Math.Abs(network.Speed * this.GearedRatio) * 1.6f;
      if (speed > 1f)
      {
        network.Speed /= speed;
        network.clientSpeed /= speed;
      }
    }

    MeshData getBaseMesh(string orient)
    {
      return ObjectCacheUtil.GetOrCreate(Api, "waterpump-" + orient + "-base", () =>
      {
        Shape shape = capi.Assets.TryGet("sfk-transportation:shapes/block/machine/waterpump/base.json").ToObject<Shape>();
        MeshData mesh;
        capi.Tesselator.TesselateShape(Block, shape, out mesh);

        int angleIdx = BlockFacing.FromCode(orient).HorizontalAngleIndex;

        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, (90 * angleIdx * GameMath.PI) / 180, 0);

        return mesh;
      });

    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
      mesher.AddMeshData(getBaseMesh(Block.Variant["side"]));

      return base.OnTesselation(mesher, tesselator);
    }
  }
}