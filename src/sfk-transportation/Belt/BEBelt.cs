using System.Runtime.InteropServices.WindowsRuntime;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace SFK.Transportation.Belt
{
  public class BEBelt : BlockEntity
  {
    BeltRenderer renderer;
    BEBehaviorMPBase mpb;
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      if (api is ICoreClientAPI capi)
      {
        renderer = new BeltRenderer(capi, this, GetBeltMesh(), mpb);

        capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "belt");
      }
    }

    public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
    {
      base.CreateBehaviors(block, worldForResolve);

      mpb = GetBehavior<BEBehaviorBeltWithAxle>();
    }

    public override void OnBlockUnloaded()
    {
      base.OnBlockUnloaded();

      renderer?.Dispose();
    }

    protected virtual MeshData GetBeltMesh()
    {
      if(Block == null) return null;
      string orientation = Block.Variant["orient"];

      return ObjectCacheUtil.GetOrCreate(Api, $"belt-{orientation}-base", () =>
      {
        string shapeVariant = orientation.Length == 1 ? "n" : "ns";
        Vec3f shapeRotation = new Vec3f(BlockBelt.GetRotatedBlockAngleFromCode(orientation), 0, 0);
        Shape shape = Api.Assets.TryGet($"sfktransportation:shapes/block/leather/belt/{shapeVariant}.json").ToObject<Shape>();
        MeshData mesh;
        ((ICoreClientAPI)Api).Tesselator.TesselateShape(Block, shape, out mesh, shapeRotation);
        return mesh;
      });
    }
    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        MeshData beltMesh = GetBeltMesh();

        ITexPositionSource texPos = tesselator.GetTexSource(Block);
        // mesh.SetTexPos(new TextureAtlasPosition())

        mesher.AddMeshData(beltMesh);

        return base.OnTesselation(mesher, tesselator);
    }
  }
}