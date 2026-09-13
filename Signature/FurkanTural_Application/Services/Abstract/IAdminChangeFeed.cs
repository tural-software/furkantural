using FurkanTural_Application.DTOs.Common;

namespace FurkanTural_Application.Services.Abstract;

public interface IAdminChangeFeed
{
    void Publish(IReadOnlyList<AdminEntityChange> changes);
}
