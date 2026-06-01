namespace ClaudeCereal.Models;

public record CerealFilter
{
    // Name search
    public string?       NameContains { get; init; }

    // Categorical
    public Manufacturer? Manufacturer { get; init; }
    public CerealType?   Type         { get; init; }
    public int?          Shelf        { get; init; }

    // Nutrition ranges
    public int?    MinCalories { get; init; }
    public int?    MaxCalories { get; init; }
    public int?    MinProtein  { get; init; }
    public int?    MaxProtein  { get; init; }
    public int?    MinFat      { get; init; }
    public int?    MaxFat      { get; init; }
    public int?    MinSodium   { get; init; }
    public int?    MaxSodium   { get; init; }
    public double? MinFiber    { get; init; }
    public double? MaxFiber    { get; init; }
    public double? MinCarbo    { get; init; }
    public double? MaxCarbo    { get; init; }
    public int?    MinSugars   { get; init; }
    public int?    MaxSugars   { get; init; }
    public int?    MinPotass   { get; init; }
    public int?    MaxPotass   { get; init; }
    public int?    MinVitamins { get; init; }
    public int?    MaxVitamins { get; init; }

    // Serving size ranges
    public double? MinWeight { get; init; }
    public double? MaxWeight { get; init; }
    public double? MinCups   { get; init; }
    public double? MaxCups   { get; init; }

    // Rating range
    public double? MinRating { get; init; }
    public double? MaxRating { get; init; }

    // Sorting
    public SortBy?    SortBy    { get; init; }
    public SortOrder? SortOrder { get; init; }

    // Pagination
    public int? Page     { get; init; }
    public int? PageSize { get; init; }

    // Soft delete
    public bool? IncludeDeleted { get; init; }

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
