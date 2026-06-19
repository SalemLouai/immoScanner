using FluentAssertions;
using FluentValidation.TestHelper;
using ImmoScorer.Application.Searches.Commands;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="RunSearchCommandValidator"/>.
/// Focus: validation rules for search criteria.
/// </summary>
public sealed class RunSearchCommandValidatorTests
{
    private readonly RunSearchCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: 100_000m,
            MaxPrice: 500_000m,
            MinArea: 30m,
            MaxArea: 100m,
            MinRooms: 2,
            MaxRooms: 4);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MissingCity_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.City)
            .WithErrorMessage("City is required.");
    }

    [Fact]
    public void Validate_MissingPostalCode_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.PostalCode)
            .WithErrorMessage("Postal code is required.");
    }

    [Fact]
    public void Validate_InvalidPostalCodeFormat_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "123", // Invalid: not 5 digits
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.PostalCode)
            .WithErrorMessage("Postal code must be a 5-digit French code.");
    }

    [Fact]
    public void Validate_NegativeMinPrice_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: -1000m,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.MinPrice)
            .WithErrorMessage("MinPrice must be positive.");
    }

    [Fact]
    public void Validate_NegativeMaxPrice_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: -5000m,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.MaxPrice)
            .WithErrorMessage("MaxPrice must be positive.");
    }

    [Fact]
    public void Validate_MinPriceGreaterThanMaxPrice_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: 500_000m,
            MaxPrice: 100_000m, // Less than MinPrice
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("MinPrice must be less than or equal to MaxPrice.");
    }

    [Fact]
    public void Validate_NegativeMinArea_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: -10m,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.MinArea)
            .WithErrorMessage("MinArea must be positive.");
    }

    [Fact]
    public void Validate_MinAreaGreaterThanMaxArea_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: 100m,
            MaxArea: 50m, // Less than MinArea
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("MinArea must be less than or equal to MaxArea.");
    }

    [Fact]
    public void Validate_NegativeMinRooms_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: -1,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.MinRooms)
            .WithErrorMessage("MinRooms must be positive.");
    }

    [Fact]
    public void Validate_ZeroMinPrice_FailsValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: 0m,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Criteria.MinPrice)
            .WithErrorMessage("MinPrice must be positive.");
    }

    [Fact]
    public void Validate_NullOptionalFields_PassesValidation()
    {
        // Arrange
        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
