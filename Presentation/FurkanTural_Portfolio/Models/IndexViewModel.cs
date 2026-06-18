namespace FurkanTural_Portfolio.Models;

public sealed class SkillViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Proficiency { get; set; }
}

public sealed class ProjectViewModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TechStack { get; set; }
    public string? GitHubUrl { get; set; }
    public string? DemoUrl { get; set; }
    public bool IsCompleted { get; set; }
}

public sealed class SongViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Artist { get; set; }
    public string? Productor { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? Lyrics { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? YouTubeMusicUrl { get; set; }
}

public sealed class ExperienceViewModel
{
    public int Id { get; set; }
    public string? Position { get; set; }
    public string? CompanyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class EducationViewModel
{
    public int Id { get; set; }
    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class IndexViewModel
{
    public IReadOnlyList<SkillViewModel> Skills { get; init; } = [];
    public IReadOnlyList<ProjectViewModel> Projects { get; init; } = [];
    public IReadOnlyList<SongViewModel> Songs { get; init; } = [];
    public IReadOnlyList<ExperienceViewModel> Experiences { get; init; } = [];
    public IReadOnlyList<EducationViewModel> Educations { get; init; } = [];
}
