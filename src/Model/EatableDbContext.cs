using Microsoft.EntityFrameworkCore;

namespace eatable;

public class EatableDbContext : DbContext
{
    public string DBPath { get; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }

    public EatableDbContext(string dbName = "eatable.db")
    {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var my_dir = "Eatable";
        DBPath = Path.Join(appdata, my_dir, dbName);
    }

    override protected void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={DBPath}");
}
