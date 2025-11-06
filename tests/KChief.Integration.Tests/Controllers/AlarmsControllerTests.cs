using System.Net;
using System.Net.Http.Json;
using KChief.Platform.API;
using KChief.Platform.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KChief.Integration.Tests.Controllers;

public class AlarmsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AlarmsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllAlarms_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/alarms");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveAlarms_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/alarms/active");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAlarm_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Title = "Test Alarm",
            Description = "Test alarm description",
            Severity = AlarmSeverity.Warning,
            VesselId = "vessel-001"
        };
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync("/api/alarms", content);
        var alarm = await response.Content.ReadFromJsonAsync<Alarm>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(alarm);
        Assert.Equal(request.Title, alarm.Title);
        Assert.Equal(AlarmStatus.Active, alarm.Status);
    }

    [Fact]
    public async Task GetAlarmById_WithValidId_ReturnsAlarm()
    {
        // Arrange - First create an alarm
        var createRequest = new
        {
            Title = "Test Alarm",
            Description = "Test alarm description",
            Severity = AlarmSeverity.Info
        };
        var createContent = JsonContent.Create(createRequest);
        var createResponse = await _client.PostAsync("/api/alarms", createContent);
        var createdAlarm = await createResponse.Content.ReadFromJsonAsync<Alarm>();

        // Act
        var response = await _client.GetAsync($"/api/alarms/{createdAlarm!.Id}");
        var alarm = await response.Content.ReadFromJsonAsync<Alarm>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(alarm);
        Assert.Equal(createdAlarm.Id, alarm.Id);
    }

    [Fact]
    public async Task AcknowledgeAlarm_WithValidId_ReturnsSuccess()
    {
        // Arrange - First create an alarm
        var createRequest = new
        {
            Title = "Test Alarm",
            Description = "Test alarm description",
            Severity = AlarmSeverity.Warning
        };
        var createContent = JsonContent.Create(createRequest);
        var createResponse = await _client.PostAsync("/api/alarms", createContent);
        var createdAlarm = await createResponse.Content.ReadFromJsonAsync<Alarm>();

        var acknowledgeRequest = new { AcknowledgedBy = "test-user" };
        var acknowledgeContent = JsonContent.Create(acknowledgeRequest);

        // Act
        var response = await _client.PostAsync($"/api/alarms/{createdAlarm!.Id}/acknowledge", acknowledgeContent);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ClearAlarm_WithValidId_ReturnsSuccess()
    {
        // Arrange - First create an alarm
        var createRequest = new
        {
            Title = "Test Alarm",
            Description = "Test alarm description",
            Severity = AlarmSeverity.Info
        };
        var createContent = JsonContent.Create(createRequest);
        var createResponse = await _client.PostAsync("/api/alarms", createContent);
        var createdAlarm = await createResponse.Content.ReadFromJsonAsync<Alarm>();

        // Act
        var response = await _client.PostAsync($"/api/alarms/{createdAlarm!.Id}/clear", null);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

