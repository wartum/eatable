using ReactiveUI;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace eatable.Ui;

public class MainWindowDataContext : ReactiveObject
{
    public Recipe? CurrentRecipe { get; set; }
    public EatableDbContext Db { get; set; }
    public ObservableCollection<Ingredient> Ingredients { get; set; }
    public ObservableCollection<Recipe> Recipes { get; set; }

    public MainWindowDataContext(EatableDbContext db)
    {
        Db = db;
        db.Database.EnsureCreated();
        Ingredients = new(Db.Ingredients.ToList());
        Recipes = new(Db.Recipes.Include(r => r.Ingredients).ToList());
    }
}
