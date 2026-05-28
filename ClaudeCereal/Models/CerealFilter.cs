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
)
{
    public IReadOnlyDictionary<string, string[]>? GetValidationErrors()
    {
        var errors = new Dictionary<string, string[]>();

        void CheckRange<T>(T? min, T? max, string minName, string maxName)
            where T : struct, IComparable<T>
        {
            if (min.HasValue && max.HasValue && min.Value.CompareTo(max.Value) > 0)
                errors[minName] = [$"{minName} must be less than or equal to {maxName}."];
        }

        CheckRange(MinCalories, MaxCalories, nameof(MinCalories), nameof(MaxCalories));
        CheckRange(MinProtein,  MaxProtein,  nameof(MinProtein),  nameof(MaxProtein));
        CheckRange(MinFat,      MaxFat,      nameof(MinFat),      nameof(MaxFat));
        CheckRange(MinSodium,   MaxSodium,   nameof(MinSodium),   nameof(MaxSodium));
        CheckRange(MinFiber,    MaxFiber,    nameof(MinFiber),    nameof(MaxFiber));
        CheckRange(MinCarbo,    MaxCarbo,    nameof(MinCarbo),    nameof(MaxCarbo));
        CheckRange(MinSugars,   MaxSugars,   nameof(MinSugars),   nameof(MaxSugars));
        CheckRange(MinPotass,   MaxPotass,   nameof(MinPotass),   nameof(MaxPotass));
        CheckRange(MinVitamins, MaxVitamins, nameof(MinVitamins), nameof(MaxVitamins));
        CheckRange(MinWeight,   MaxWeight,   nameof(MinWeight),   nameof(MaxWeight));
        CheckRange(MinCups,     MaxCups,     nameof(MinCups),     nameof(MaxCups));
        CheckRange(MinRating,   MaxRating,   nameof(MinRating),   nameof(MaxRating));

        return errors.Count > 0 ? errors : null;
    }
}
