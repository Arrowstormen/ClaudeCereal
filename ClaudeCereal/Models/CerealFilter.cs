namespace ClaudeCereal.Models;

public record CerealFilter(
    // Name search
    string? NameContains,
    // Categorical
    Manufacturer? Manufacturer,
    CerealType? Type,
    int? Shelf,
    // Nutrition ranges
    int? MinCalories,
    int? MaxCalories,
    int? MinProtein,
    int? MaxProtein,
    int? MinFat,
    int? MaxFat,
    int? MinSodium,
    int? MaxSodium,
    double? MinFiber,
    double? MaxFiber,
    double? MinCarbo,
    double? MaxCarbo,
    int? MinSugars,
    int? MaxSugars,
    int? MinPotass,
    int? MaxPotass,
    int? MinVitamins,
    int? MaxVitamins,
    // Serving size ranges
    double? MinWeight,
    double? MaxWeight,
    double? MinCups,
    double? MaxCups,
    // Rating range
    double? MinRating,
    double? MaxRating,
    // Sorting
    SortBy? SortBy,
    SortOrder? SortOrder,
    // Pagination
    int? Page,
    int? PageSize
);
