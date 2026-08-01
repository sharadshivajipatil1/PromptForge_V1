using System.Security.Claims;
using System.Text.Json;
using HospitalityAI.Agents;
using HospitalityAI.Api.Controllers;
using HospitalityAI.Domain.Configuration;
using HospitalityAI.Domain.Dtos;
using HospitalityAI.Domain.Enums;
using HospitalityAI.Domain.Interfaces;
using HospitalityAI.Domain.Interfaces.Configuration;
using HospitalityAI.Domain.Models;
using HospitalityAI.Infrastructure.Authentication;
using HospitalityAI.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using TaskStatus = HospitalityAI.Domain.Enums.TaskStatus;

namespace HospitalityAI.Tests;

public class StaffControllerTests
{
    [Fact]
    public async Task UpdateProfile_UpdatesStoredStaffAndReturnsUpdatedProfile()
    {
        var dataStore = new Mock<IDataStore>();
        var llmClient = new Mock<ILlmClient>();
        var forecastService = new Mock<IForecastService>();
        var operationsAgent = new OperationsAgent(dataStore.Object, llmClient.Object);

        var staff = new StaffUser
        {
            Id = "staff-2",
            Username = "frontdesk",
            FullName = "Dana Reyes",
            Department = "Front Desk",
            Role = StaffRole.FrontDesk
        };

        dataStore
            .Setup(store => store.GetStaffByUsernameAsync("frontdesk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        dataStore
            .Setup(store => store.SaveStaffUserAsync(It.IsAny<StaffUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffUser saved, CancellationToken _) => saved);

        var controller = new StaffController(dataStore.Object, operationsAgent, forecastService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("username", "frontdesk") }, "TestAuth"));

        var result = await controller.UpdateProfile(new StaffController.UpdateStaffProfileRequest
        {
            FullName = "Dana L. Reyes",
            Department = "Operations"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<StaffUserDto>(okResult.Value);

        Assert.Equal("Dana L. Reyes", profile.FullName);
        Assert.Equal("Operations", profile.Department);
        Assert.Equal(StaffRole.FrontDesk, profile.Role);
        dataStore.Verify(store => store.SaveStaffUserAsync(It.Is<StaffUser>(saved => saved.FullName == "Dana L. Reyes" && saved.Department == "Operations"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveTaskAsync_PersistsTasksToJsonFile_ForStaffDashboardUse()
    {
        var filePath = ResolveTaskStorePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var referenceDataLoader = new Mock<IReferenceDataLoader>();
        referenceDataLoader.Setup(loader => loader.LoadSeedDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeedDataSettings());

        var passwordHasher = new PasswordHasher();
        var dataStore = new InMemoryDataStore(referenceDataLoader.Object, passwordHasher);

        var task = new StaffTask
        {
            Type = TaskType.GuestRequest,
            RoomNumber = "812",
            Description = "Guest requested spa booking",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.High,
            AssignedTo = "Dana Reyes",
            Department = "Front Desk"
        };

        await dataStore.SaveTaskAsync(task, CancellationToken.None);

        Assert.True(File.Exists(filePath));

        var json = await File.ReadAllTextAsync(filePath);
        var store = JsonSerializer.Deserialize<TaskStoreSnapshot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(store);
        Assert.Contains(store!.Tasks, saved => saved.Description == "Guest requested spa booking");

        var tasks = await dataStore.GetTasksAsync(null, null, 1, 100, CancellationToken.None);
        Assert.Contains(tasks, saved => saved.Description == "Guest requested spa booking");

        File.Delete(filePath);
    }

    [Fact]
    public async Task CreateAndCompleteTask_PersistsChangesToJsonStore()
    {
        var filePath = ResolveTaskStorePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var referenceDataLoader = new Mock<IReferenceDataLoader>();
        referenceDataLoader.Setup(loader => loader.LoadSeedDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeedDataSettings());

        var passwordHasher = new PasswordHasher();
        var dataStore = new InMemoryDataStore(referenceDataLoader.Object, passwordHasher);
        var llmClient = new Mock<ILlmClient>();
        var forecastService = new Mock<IForecastService>();
        var operationsAgent = new OperationsAgent(dataStore, llmClient.Object);
        var controller = new StaffController(dataStore, operationsAgent, forecastService.Object);

        var created = await controller.CreateTask(new StaffController.CreateTaskRequest
        {
            Type = TaskType.RoomService.ToString(),
            Description = "Deliver extra towels",
            RoomNumber = "220",
            SlaMinutes = 30,
            AssignedTo = "Dana Reyes",
            Department = "Front Desk"
        }, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(created);
        var taskDto = Assert.IsType<TaskDto>(createdResult.Value);
        Assert.Equal("Deliver extra towels", taskDto.Description);

        var before = await File.ReadAllTextAsync(filePath);
        Assert.Contains("Deliver extra towels", before);

        var completeResult = await controller.CompleteTask(taskDto.Id, CancellationToken.None);
        var completeOk = Assert.IsType<OkObjectResult>(completeResult);
        var completedTask = Assert.IsType<TaskDto>(completeOk.Value);
        Assert.Equal(TaskStatus.Completed, completedTask.Status);

        var after = await File.ReadAllTextAsync(filePath);
        Assert.Contains("\"Status\": 2", after);

        File.Delete(filePath);
    }

    [Fact]
    public async Task UpdateTaskStatus_ChangesStatusAndPersistsToJsonStore()
    {
        var filePath = ResolveTaskStorePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var referenceDataLoader = new Mock<IReferenceDataLoader>();
        referenceDataLoader.Setup(loader => loader.LoadSeedDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeedDataSettings());

        var passwordHasher = new PasswordHasher();
        var dataStore = new InMemoryDataStore(referenceDataLoader.Object, passwordHasher);
        var llmClient = new Mock<ILlmClient>();
        var forecastService = new Mock<IForecastService>();
        var operationsAgent = new OperationsAgent(dataStore, llmClient.Object);
        var controller = new StaffController(dataStore, operationsAgent, forecastService.Object);

        await dataStore.SaveTaskAsync(new StaffTask
        {
            Id = "status-task-1",
            Type = TaskType.Housekeeping,
            RoomNumber = "405",
            Description = "Change status test",
            Status = TaskStatus.Pending,
            Priority = TaskPriority.Medium,
            AssignedTo = "Dana Reyes",
            Department = "Front Desk"
        }, CancellationToken.None);

        var result = await controller.UpdateTaskStatus("status-task-1", new StaffController.UpdateTaskStatusRequest
        {
            Status = "Completed"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var taskDto = Assert.IsType<TaskDto>(okResult.Value);
        Assert.Equal(TaskStatus.Completed, taskDto.Status);

        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("\"Status\": 2", json);

        File.Delete(filePath);
    }

    [Fact]
    public async Task GuestCreatedTicket_StoresCreatorAndRemark_ForGuestDashboardReference()
    {
        var referenceDataLoader = new Mock<IReferenceDataLoader>();
        referenceDataLoader.Setup(loader => loader.LoadSeedDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeedDataSettings());

        var passwordHasher = new PasswordHasher();
        var dataStore = new InMemoryDataStore(referenceDataLoader.Object, passwordHasher);
        var ticket = new ConciergeTicket
        {
            GuestId = "guest-42",
            GuestName = "Ava Patil",
            RoomNumber = "812",
            Message = "Airport transfer request",
            Status = "Open",
            CreatedBy = "Guest",
            Remark = "Guest created this request from the dashboard."
        };

        await dataStore.SaveTicketAsync(ticket, CancellationToken.None);

        var saved = await dataStore.GetTicketsAsync(null, 1, 50, CancellationToken.None);
        var entry = Assert.Single(saved.Items);

        Assert.Equal("Guest", entry.CreatedBy);
        Assert.Equal("Guest created this request from the dashboard.", entry.Remark);
        Assert.Equal("Ava Patil", entry.GuestName);
    }

    private static string ResolveTaskStorePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "HospitalityAI.sln");
            if (File.Exists(candidate))
            {
                return Path.Combine(directory.FullName, "hospitality-task-store.json");
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "hospitality-task-store.json");
    }

    private sealed class TaskStoreSnapshot
    {
        public List<StaffTask> Tasks { get; set; } = new();
    }
}
