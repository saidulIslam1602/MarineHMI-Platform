using System.Net;
using System.Net.Http.Json;
using KChief.Platform.API;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KChief.Integration.Tests.Controllers;

public class EnginesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EnginesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartEngine_WithStoppedEngine_ReturnsSuccess()
    {
        // Arrange
        var vesselId = "vessel-002";
        var engineId = "engine-003";

        // Act
        var response = await _client.PostAsync($"/api/engines/{vesselId}/engines/{engineId}/start", null);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StopEngine_WithRunningEngine_ReturnsSuccess()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";

        // Act
        var response = await _client.PostAsync($"/api/engines/{vesselId}/engines/{engineId}/stop", null);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetEngineRpm_WithValidRpm_ReturnsSuccess()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";
        var request = new { Rpm = 800 };
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync($"/api/engines/{vesselId}/engines/{engineId}/rpm", content);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetEngineRpm_WithInvalidRpm_ReturnsBadRequest()
    {
        // Arrange
        var vesselId = "vessel-001";
        var engineId = "engine-001";
        var request = new { Rpm = 2000 }; // Exceeds max RPM
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync($"/api/engines/{vesselId}/engines/{engineId}/rpm", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

