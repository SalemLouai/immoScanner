using FluentValidation;

namespace ImmoScorer.Application.Searches.Commands;

/// <summary>FluentValidation validator for <see cref="RunSearchCommand"/>.</summary>
public sealed class RunSearchCommandValidator : AbstractValidator<RunSearchCommand>
{
    /// <summary>Defines validation rules for <see cref="RunSearchCommand"/>.</summary>
    public RunSearchCommandValidator()
    {
        RuleFor(x => x.Criteria.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.Criteria.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .Matches(@"^\d{5}$").WithMessage("Postal code must be a 5-digit French code.");

        RuleFor(x => x.Criteria.MinPrice)
            .GreaterThan(0).When(x => x.Criteria.MinPrice.HasValue)
            .WithMessage("MinPrice must be positive.");

        RuleFor(x => x.Criteria.MaxPrice)
            .GreaterThan(0).When(x => x.Criteria.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be positive.");

        RuleFor(x => x)
            .Must(x => x.Criteria.MinPrice is null || x.Criteria.MaxPrice is null
                       || x.Criteria.MinPrice <= x.Criteria.MaxPrice)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");

        RuleFor(x => x.Criteria.MinArea)
            .GreaterThan(0).When(x => x.Criteria.MinArea.HasValue)
            .WithMessage("MinArea must be positive.");

        RuleFor(x => x.Criteria.MaxArea)
            .GreaterThan(0).When(x => x.Criteria.MaxArea.HasValue)
            .WithMessage("MaxArea must be positive.");

        RuleFor(x => x)
            .Must(x => x.Criteria.MinArea is null || x.Criteria.MaxArea is null
                       || x.Criteria.MinArea <= x.Criteria.MaxArea)
            .WithMessage("MinArea must be less than or equal to MaxArea.");

        RuleFor(x => x.Criteria.MinRooms)
            .GreaterThan(0).When(x => x.Criteria.MinRooms.HasValue)
            .WithMessage("MinRooms must be positive.");

        RuleFor(x => x.Criteria.MaxRooms)
            .GreaterThan(0).When(x => x.Criteria.MaxRooms.HasValue)
            .WithMessage("MaxRooms must be positive.");
    }
}
