using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace SFK.Steamworks
{
  public class RollingRecipeIngredient : CraftingRecipeIngredient
  {
    public new RollingRecipeIngredient Clone()
    {
      RollingRecipeIngredient stack = new RollingRecipeIngredient()
      {
        Code = Code.Clone(),
        Type = Type,
        Name = Name,
        Quantity = Quantity,
        IsWildCard = IsWildCard,
        IsTool = IsTool,
        AllowedVariants = AllowedVariants == null ? null : (string[])AllowedVariants.Clone(),
        ResolvedItemstack = ResolvedItemstack?.Clone(),
        ReturnedStack = ReturnedStack?.Clone()
      };

      if (Attributes != null) stack.Attributes = Attributes.Clone();

      return stack;
    }
  }

  public class RollingOutputStack : JsonItemStack
  {
    public new RollingOutputStack Clone()
    {
      RollingOutputStack stack = new RollingOutputStack()
      {
        Code = Code.Clone(),
        ResolvedItemstack = ResolvedItemstack?.Clone(),
        StackSize = StackSize,
        Type = Type,
      };

      if (Attributes != null) stack.Attributes = Attributes.Clone();

      return stack;
    }
  }

  public class RollingRecipe : IByteSerializable, IRecipeBase<RollingRecipe>
  {
    public int RecipeId;

    /// <summary>
    /// ...or alternatively for recipes with multiple ingredients
    /// </summary>
    public RollingRecipeIngredient Ingredient;
    public RollingOutputStack Output;
    public AssetLocation Name { get; set; }
    public bool Enabled { get; set; } = true;

    public string Code;

    IRecipeIngredient[] IRecipeBase<RollingRecipe>.Ingredients => new RollingRecipeIngredient[] { Ingredient };
    IRecipeOutput IRecipeBase<RollingRecipe>.Output => Output;

    public bool Matches(ItemSlot inputSlot)
    {
      return Ingredient.SatisfiesAsIngredient(inputSlot.Itemstack);
    }

    public bool TryCraftNow(ICoreAPI api, ItemSlot inputSlot)
    {
      ItemStack inputStack = inputSlot.Itemstack;
      ItemStack mixedStack = Output.ResolvedItemstack.Clone();
      mixedStack.StackSize = Output.Quantity;

      float inputTemp = inputStack.Collectible.GetTemperature(api.World, inputStack);
      mixedStack.Collectible.SetTemperature(api.World, mixedStack, inputTemp);

      inputSlot.TakeOut(Ingredient.Quantity);
      inputSlot.MarkDirty();

      return true;
    }

    #region IByteSerializable

    public void ToBytes(BinaryWriter writer)
    {
      writer.Write(Code);

      Ingredient.ToBytes(writer);

      Output.ToBytes(writer);
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
      Code = reader.ReadString();

      Ingredient = new RollingRecipeIngredient();
      Ingredient.FromBytes(reader, resolver);
      Ingredient.Resolve(resolver, "Rolling Recipe (FromBytes)");

      Output = new RollingOutputStack();
      Output.FromBytes(reader, resolver.ClassRegistry);
      Output.Resolve(resolver, "Rolling Recipe (FromBytes)");
    }

    #endregion

    /// <summary>
    /// Resolves Wildcards in the ingredients
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
    {
      Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

      if (Ingredient == null) return mappings;

      int wildcardStartLen = Ingredient.Code.Path.IndexOf("*");
      int wildcardEndLen = Ingredient.Code.Path.Length - wildcardStartLen - 1;

      List<string> codes = new List<string>();

      if (Ingredient.Type == EnumItemClass.Block)
      {
        for (int i = 0; i < world.Blocks.Count; i++)
        {
          if (world.Blocks[i].Code == null || world.Blocks[i].IsMissing) continue;

          if (WildcardUtil.Match(Ingredient.Code, world.Blocks[i].Code))
          {
            string code = world.Blocks[i].Code.Path.Substring(wildcardStartLen);
            string codepart = code.Substring(0, code.Length - wildcardEndLen);
            if (Ingredient.AllowedVariants != null && !Ingredient.AllowedVariants.Contains(codepart)) continue;

            codes.Add(codepart);

          }
        }
      }
      else
      {
        for (int i = 0; i < world.Items.Count; i++)
        {
          if (world.Items[i].Code == null || world.Items[i].IsMissing) continue;

          if (WildcardUtil.Match(Ingredient.Code, world.Items[i].Code))
          {
            string code = world.Items[i].Code.Path.Substring(wildcardStartLen);
            string codepart = code.Substring(0, code.Length - wildcardEndLen);
            if (Ingredient.AllowedVariants != null && !Ingredient.AllowedVariants.Contains(codepart)) continue;

            codes.Add(codepart);
          }
        }
      }

      mappings[Ingredient.Name] = codes.ToArray();

      return mappings;
    }

    public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
    {
      bool ok = true;

      bool iOk = Ingredient.Resolve(world, sourceForErrorLogging);
      ok &= iOk;

      ok &= Output.Resolve(world, sourceForErrorLogging);

      return ok;
    }

    public RollingRecipe Clone()
    {
      return new RollingRecipe()
      {
        Output = Output.Clone(),
        Code = Code,
        Enabled = Enabled,
        Name = Name,
        RecipeId = RecipeId,
        Ingredient = Ingredient.Clone()
      };
    }
  }
}
