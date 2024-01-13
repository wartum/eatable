namespace eatable;
public class Ingredient
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Available { get; set; }
    public List<Recipe> Recipes { get; set; } = new();
}
