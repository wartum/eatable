using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Transactions;

namespace eatable.Ui;

public partial class MainWindow : Window
{
    private MainWindowDataContext _dataContext;
    private bool _recipesDisplayRightClicked = false;

    public MainWindow() : this(new EatableDbContext()) { }

    public MainWindow(EatableDbContext db)
    {
        _dataContext = new MainWindowDataContext(db);
        DataContext = _dataContext;

        InitializeComponent();
        InitializeMyComponents();
    }

    private void InitializeMyComponents()
    {
        RecipesDisplay.AddHandler(PointerPressedEvent, RecipePointerPressed, RoutingStrategies.Tunnel);
        RecipesDisplay.AddHandler(PointerReleasedEvent, RecipePointerReleased, RoutingStrategies.Tunnel);

        foreach (var r in _dataContext.Recipes)
        {
            TextBlock widget = new()
            {
                Text = r.Name,
            };

            RecipesDisplay.Items.Add(widget);
        }

        foreach (var i in _dataContext.Ingredients)
        {
            ToggleButton widget = new()
            {
                Content = i.Name,
                IsChecked = i.Available,
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            widget.Click += IngredientClicked;
            widget.PointerPressed += IngredientPressed;

            IngredientsDisplay.Children.Add(widget);
        }
    }
    
    private Recipe? GetCurrentRecipe()
    {
        return _dataContext.CurrentRecipe;
    }

    private void RefreshRecipeView()
    {
        var currentRecipe = GetCurrentRecipe();
        if (currentRecipe != null) RefreshRecipeView(currentRecipe);
    }

    private void RefreshRecipeView(Recipe recipe)
    {
        var ingredients = recipe.Ingredients;

        RecipeName.Text = recipe.Name;
        RecipeInfo.Text = recipe.Info;

        RecipeIngredients.Children.Clear();
        RecipeMissingIngredients.Children.Clear();

        foreach (var i in ingredients)
        {
            var button = new Button()
            {
                Content = i.Name,
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(20),
            };
            button.Click += IngredientInsideRecipeViewClicked;
            RecipeIngredients.Children.Add(button);

            if (!i.Available)
            {
                button = new Button()
                {
                    Content = i.Name,
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(20),
                };
                button.Click += IngredientInsideRecipeViewClicked;
                RecipeMissingIngredients.Children.Add(button);
            }
        }
        _dataContext.CurrentRecipe = recipe;
    }

    // *******************************
    // Event handlers
    // *******************************
    public void RecipeNamePointerLeft(object? source, PointerEventArgs args)
    {
        var widget = source as TextBox;
        if (widget is null) return;

        if (!widget.IsFocused) return;

        var currentRecipe = GetCurrentRecipe();
        if (currentRecipe == null) return;

        var displayedItem = RecipesDisplay.Items.Where(i => (i as TextBlock)?.Text == currentRecipe.Name).FirstOrDefault();

        currentRecipe.Name = RecipeName.Text;
        _dataContext.Db.Update(currentRecipe);
        _dataContext.Db.SaveChanges();
        if (displayedItem is not null)
            (displayedItem as TextBlock).Text = currentRecipe.Name;

        Focus();
    }

    public void RecipeInfoPointerLeft(object? source, PointerEventArgs args)
    {
        var widget = source as TextBox;
        if (widget is null) return;

        if (!widget.IsFocused) return;

        var currentRecipe = GetCurrentRecipe();
        if (currentRecipe == null) return;
        currentRecipe.Info = RecipeInfo.Text;
        _dataContext.Db.Update(currentRecipe);
        _dataContext.Db.SaveChanges();
        Focus();
    }

    public void RecipePointerPressed(object? source, PointerPressedEventArgs args)
    {
        _recipesDisplayRightClicked = args.GetCurrentPoint(source as Control).Properties.IsRightButtonPressed;
    }

    public void RecipePointerReleased(object? source, PointerEventArgs args)
    {
        var recipeName = (RecipesDisplay.SelectedItem as TextBlock)?.Text as string;
        if (recipeName is null) return;

        var recipe = _dataContext.Recipes.Where(r => r.Name == recipeName).FirstOrDefault();
        if (recipe is null) return;

        if (!_recipesDisplayRightClicked)
        {
            RefreshRecipeView(recipe);
        }
        else
        {
            _dataContext.Recipes.Remove(recipe);
            _dataContext.Db.Remove(recipe);
            _dataContext.Db.SaveChanges();
            var itemToRemove = RecipesDisplay.Items.Where(i => (i as TextBlock)?.Text == recipe.Name).FirstOrDefault();
            if (itemToRemove is not null) RecipesDisplay.Items.Remove(itemToRemove);
        }
    }

    public void NewRecipeButtonClicked(object? source, RoutedEventArgs args)
    {
        var newRecipeName = NewRecipeName.Text;
        if (String.IsNullOrEmpty(newRecipeName)) return;
        if (_dataContext.Recipes.Any(r => r.Name == newRecipeName)) return;

        var recipe = new Recipe() { Name = newRecipeName };
        _dataContext.Db.Add(recipe);
        _dataContext.Db.SaveChanges();
        _dataContext.Recipes.Add(recipe);

        RecipesDisplay.Items.Add(new TextBlock() { Text = recipe.Name});
    }

    public void NewIngredientButtonClicked(object? source, RoutedEventArgs args)
    {
        var newIngredientName = NewIngredientName.Text;
        if (String.IsNullOrEmpty(newIngredientName)) return;
        if (_dataContext.Ingredients.Any(r => r.Name == newIngredientName)) return;

        var ingredient = new Ingredient() { Name = newIngredientName };
        _dataContext.Db.Add(ingredient);
        _dataContext.Db.SaveChanges();
        _dataContext.Ingredients.Add(ingredient);

        ToggleButton widget = new()
        {
            Content = ingredient.Name,
            IsChecked = ingredient.Available,
            Margin = new Thickness(5),
            CornerRadius = new CornerRadius(20),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        };
        widget.Click += IngredientClicked;
        widget.PointerPressed += IngredientPressed;

        IngredientsDisplay.Children.Add(widget);
    }


    public void IngredientPressed(object? source, PointerPressedEventArgs args)
    {
        if (source is not ToggleButton button) return;
        var ingredientName = button.Content as string;
        if (String.IsNullOrEmpty(ingredientName)) return;

        var ingredient = _dataContext.Ingredients.Where(i => i.Name == ingredientName).FirstOrDefault();
        if (ingredient == null) return;

        if (args.GetCurrentPoint(source as Control).Properties.IsRightButtonPressed)
        {
            var widget = IngredientsDisplay.Children.Where(w => ((ToggleButton)w).Content as string == ingredientName).FirstOrDefault();
            if (widget == null) return;

            foreach (var r in _dataContext.Recipes)
            {
                r.Ingredients.Remove(ingredient);
            }

            _dataContext.Ingredients.Remove(ingredient);
            IngredientsDisplay.Children.Remove(widget);
            _dataContext.Db.Remove(ingredient);
            _dataContext.Db.SaveChanges();
            RefreshRecipeView();
        }
        else if (args.GetCurrentPoint(source as Control).Properties.IsMiddleButtonPressed)
        {
            var recipe = GetCurrentRecipe();
            if (recipe == null) return;

            if (!recipe.Ingredients.Contains(ingredient))
            {
                recipe.Ingredients.Add(ingredient);
                _dataContext.Db.Update(recipe);
                _dataContext.Db.SaveChanges();
                RefreshRecipeView();
            }
        }
    }

    public void IngredientClicked(object? source, RoutedEventArgs args)
    {
        var toggleButton = source as ToggleButton;
        if (toggleButton is null) return;

        Ingredient? ingredient = _dataContext.Ingredients.Where(i => i.Name == toggleButton.Content as string).FirstOrDefault();
        if (ingredient == null) return;

        ingredient.Available = !ingredient.Available;
        _dataContext.Db.Update(ingredient);
        _dataContext.Db.SaveChanges();
        RefreshRecipeView();
    }

    public void IngredientInsideRecipeViewClicked(object? source, RoutedEventArgs args)
    {
        var button = source as Button;
        if (button is null) return;
        
        var ingredient = _dataContext.Ingredients.Where(i => i.Name == button.Content).FirstOrDefault();
        if (ingredient == null) return;

        var recipe = GetCurrentRecipe();
        if (recipe == null) return;

        recipe.Ingredients.Remove(ingredient);
        _dataContext.Db.Update(recipe);
        _dataContext.Db.SaveChanges();
        RefreshRecipeView();
    }
}