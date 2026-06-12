using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Factory/Recipe")]
public class RecipeData : ScriptableObject
{
    public string RecipeName = "New Recipe";
    
    [Header("Requirements")]
    public List<ItemStack> Inputs = new List<ItemStack>();
    
    [Header("Results")]
    public List<ItemStack> Outputs = new List<ItemStack>();
    
    [Header("Settings")]
    public float ProcessTime = 2f;
}
