namespace eatable;

public class Recipe
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public List<Ingredient> Ingredients { get; set; } = new();
}
