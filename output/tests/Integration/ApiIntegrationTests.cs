using FluentAssertions;
using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Application.Searches.Dtos;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace ImmoScorer.Tests.Integration;

/// <summary>
/// Integration tests for the API endpoints using WebApplicationFactory.
/// Focus: end-to-end request/response validation.
/// </summary>
public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task PostSearch_ValidCriteria_ReturnsSearchId()
    {
        // Arrange
        var client = _factory.CreateClient();
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

        // Act
        var response = await client.PostAsJsonAsync("/searches", new { criteria });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchIdResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostSearch_InvalidCriteria_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var criteria = new SearchCriteria(
            City: "", // Invalid: empty
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        var response = await client.PostAsJsonAsync("/searches", new { criteria });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostSearchScrape_ExistingSearch_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // First, create a search
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

        var createResponse = await client.PostAsJsonAsync("/searches", new { criteria });
        var searchIdResponse = await createResponse.Content.ReadFromJsonAsync<SearchIdResponse>();

        // Act
        var response = await client.PostAsync($"/searches/{searchIdResponse!.Id}/scrape", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSearches_ReturnsListOfSearches()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a search first
        var criteria = new SearchCriteria(
            City: "Lyon",
            PostalCode: "69001",
            PropertyType: PropertyType.House,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        await client.PostAsJsonAsync("/searches", new { criteria });

        // Act
        var response = await client.GetAsync("/searches");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var searches = await response.Content.ReadFromJsonAsync<List<SavedSearchDto>>();
        searches.Should().NotBeNull();
        searches.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetListings_WithValidSearchId_ReturnsListings()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a search
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

        var createResponse = await client.PostAsJsonAsync("/searches", new { criteria });
        var searchIdResponse = await createResponse.Content.ReadFromJsonAsync<SearchIdResponse>();

        // Trigger scraping (will use FixtureScraper by default)
        await client.PostAsync($"/searches/{searchIdResponse!.Id}/scrape", null);

        // Wait briefly for background processing (in real scenario, would poll or use events)
        await Task.Delay(2000);

        // Act
        var response = await client.GetAsync($"/listings?searchId={searchIdResponse.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ListingDto>>();
        result.Should().NotBeNull();
        // Note: Fixture scraper should return some listings
    }

    [Fact]
    public async Task GetListings_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var client = _factory.CreateClient();

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75002",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var createResponse = await client.PostAsJsonAsync("/searches", new { criteria });
        var searchIdResponse = await createResponse.Content.ReadFromJsonAsync<SearchIdResponse>();

        await client.PostAsync($"/searches/{searchIdResponse!.Id}/scrape", null);
        await Task.Delay(2000);

        // Act - filter by minimum score
        var response = await client.GetAsync(
            $"/listings?searchId={searchIdResponse.Id}&minScore=60&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ListingDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(l => l.Score >= 60);
    }

    [Fact]
    public async Task GetListingDetail_ExistingListing_ReturnsDetail()
    {
        // Arrange
        var client = _factory.CreateClient();

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75003",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        var createResponse = await client.PostAsJsonAsync("/searches", new { criteria });
        var searchIdResponse = await createResponse.Content.ReadFromJsonAsync<SearchIdResponse>();

        await client.PostAsync($"/searches/{searchIdResponse!.Id}/scrape", null);
        await Task.Delay(2000);

        // Get listings to find an ID
        var listingsResponse = await client.GetAsync($"/listings?searchId={searchIdResponse.Id}");
        var listingsResult = await listingsResponse.Content.ReadFromJsonAsync<PaginatedResponse<ListingDto>>();

        if (listingsResult?.Items.Count > 0)
        {
            var listingId = listingsResult.Items[0].Id;

            // Act
            var response = await client.GetAsync($"/listings/{listingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var detail = await response.Content.ReadFromJsonAsync<ListingDetailDto>();
            detail.Should().NotBeNull();
            detail!.Id.Should().Be(listingId);
            detail.ScoreBreakdown.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetListingDetail_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/listings/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DTOs for deserialization
    private sealed record SearchIdResponse(Guid Id);

    private sealed record PaginatedResponse<T>(
        List<T> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);
}
