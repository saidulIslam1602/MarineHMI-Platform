using System.Net;
using System.Net.Http.Json;
using KChief.Platform.API;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KChief.Integration.Tests.Controllers;

public class VesselsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public VesselsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllVessels_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/vessels");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllVessels_ReturnsJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/api/vessels");

        // Assert
        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);
    }

    [Fact]
    public async Task GetAllVessels_ReturnsVesselsList()
    {
        // Act
        var response = await _client.GetAsync("/api/vessels");
        var vessels = await response.Content.ReadFromJsonAsync<List<Vessel>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(vessels);
        Assert.NotEmpty(vessels);
    }

    [Fact]
    public async Task GetVesselById_WithValidId_ReturnsVessel()
    {
        // Arrange
        var vesselId = "vessel-001";

        // Act
        var response = await _client.GetAsync($"/api/vessels/{vesselId}");
        var vessel = await response.Content.ReadFromJsonAsync<Vessel>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(vessel);
        Assert.Equal(vesselId, vessel.Id);
    }

    [Fact]
    public async Task GetVesselById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var vesselId = "invalid-vessel-id";

        // Act
        var response = await _client.GetAsync($"/api/vessels/{vesselId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVesselEngines_WithValidVesselId_ReturnsEngines()
    {
        // Arrange
        var vesselId = "vessel-001";

        // Act
        var response = await _client.GetAsync($"/api/vessels/{vesselId}/engines");
        var engines = await response.Content.ReadFromJsonAsync<List<Engine>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(engines);
        Assert.NotEmpty(engines);
    }

    [Fact]
    public async Task GetVesselSensors_WithValidVesselId_ReturnsSensors()
    {
        // Arrange
        var vesselId = "vessel-001";

        // Act
        var response = await _client.GetAsync($"/api/vessels/{vesselId}/sensors");
        var sensors = await response.Content.ReadFromJsonAsync<List<Sensor>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(sensors);
        Assert.NotEmpty(sensors);
    }
}

