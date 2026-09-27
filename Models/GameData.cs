namespace BlazorApp1.Models;

public record Resource(
    string Id,
    string Name,
    string Icon,
    string Category,
    string Color
);

public record Building(
    string Id,
    string Name,
    string Icon,
    decimal? Power,
    int? Workers
);

public record RecipeItem(
    string ResourceId,
    decimal Amount
);

public record Recipe(
    string Id,
    string Name,
    string BuildingId,
    decimal? Time,
    List<RecipeItem> Inputs,
    List<RecipeItem> Outputs
);







