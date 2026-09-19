using FurkanTural_Application.DTOs.Log;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FurkanTural_Business.Tests;

internal sealed class ActivityJournal
{
    public List<string> Events { get; } = [];

    public List<CreateLogDto> Logs { get; } = [];

    public ActivityLogger Logger(IClock clock)
    {
        var service = new Mock<ILogService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateLogDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateLogDto, CancellationToken>((dto, _) =>
            {
                Logs.Add(dto);
                Events.Add($"log:{dto.Level}");
            })
            .ReturnsAsync(Result<LogDto>.Ok(new LogDto()));

        return new ActivityLogger(service.Object, Mock.Of<IHttpContextAccessor>(), clock);
    }

    public void Mail() => Events.Add("mail");

    public void Save() => Events.Add("save");
}
