namespace FurkanTural_Application.DTOs.Common;

public sealed record AdminListChangeDto(string Kind, int ActorId, int Added, int Changed);

public sealed record AdminListsChangedDto(IReadOnlyList<AdminListChangeDto> Changes);

public sealed record AdminSessionDto(int UserId);
