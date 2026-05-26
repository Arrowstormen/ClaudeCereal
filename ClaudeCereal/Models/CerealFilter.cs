namespace ClaudeCereal.Models;

public record CerealFilter(
    Manufacturer? Manufacturer,
    CerealType? Type,
    string? Name,
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
    double? MinRating,
    double? MaxRating
);
