using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SFK.Steamworks
{
  public static class SFKApiAdditions
  {
    public static List<RollingRecipe> GetRollingRecipes(this ICoreAPI api)
    {
      return api.ModLoader.GetModSystem<SFKRecipeRegistry>().RollingRecipes;
    }

    public static void RegisterRollingRecipe(this ICoreServerAPI api, RollingRecipe r)
    {
      api.ModLoader.GetModSystem<SFKRecipeRegistry>().RegisterRollingRecipe(r);
    }
  }

  public class SFKRecipeRegistry : ModSystem
  {
    /// <summary>
    /// List of all loaded rolling recipes
    /// </summary>
    public List<RollingRecipe> RollingRecipes = new List<RollingRecipe>();

    public override void Start(ICoreAPI api)
    {
      RollingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<RollingRecipe>>("rollingrecipes").Recipes;
    }

    /// <summary>
    /// Registers a new rolling recipe. These are sent to the client during connect, so only need to register them on the server side.
    /// </summary>
    /// <param name="recipe"></param>
    public void RegisterRollingRecipe(RollingRecipe recipe)
    {
      RollingRecipes.Add(recipe);
    }
  }
}
