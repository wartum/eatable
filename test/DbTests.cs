using NUnit.Framework;
using eatable;
using Microsoft.EntityFrameworkCore;

namespace test;

public class DbTests
{
    private EatableDbContext _db = new EatableDbContext("TestDb.db");
    private static Ingredient[] _sampleIngredients =
    {
        new Ingredient() { Name="Kurczak", Available=true },
        new Ingredient() { Name="Garam Masala", Available=true },
        new Ingredient() { Name="Pomidor", Available=true },
    };
    private static Recipe[] _sampleRecipes =
    {
        new Recipe() { Name = "Rosó³", Info = "Gotuj 5 godzin", Ingredients = new() 
            {
                _sampleIngredients[0]
            }
        },
        new Recipe() { Name = "Kura Kary",Ingredients = new()
            {
                _sampleIngredients[0],
                _sampleIngredients[1]
            }
        },
    };

    [SetUp]
    public void Setup()
    {
        _db.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Database.EnsureDeleted();
    }

    private void PopulateDb()
    {
        foreach (var i in _sampleIngredients)
            _db.Add(i);
        foreach (var r in _sampleRecipes)
            _db.Add(r);
        _db.SaveChanges();
    }

    [Test]
    public void RecipeIsPopulatedWithIngredients()
    {
        PopulateDb();
        EatableDbContext db = new EatableDbContext("TestDb.db");
        var recipes = db.Recipes.Include(r => r.Ingredients).ToArray();
        Assert.That(recipes.Length, Is.EqualTo(_sampleRecipes.Length));
        foreach (var r in recipes)
            Assert.That(r.Ingredients.Count() > 0);
    }
}