using System.ComponentModel.DataAnnotations;

namespace ChainResource.Runner.Configuration;

public class WebStorageOptions
{
    public const string SectionName = "WebStorageOptions";

    [Required]
    public string AppId { get; set; }

    [Required, Url] public string BaseUrl { get; set; } = "https://openexchangerates.org/api/";
}

public class FileStorageOptions
{
    public const string SectionName = "FileStorageOptions";

    [Required]
    public string FolderPath { get; set; }

    [Required]
    public string FileName { get; set; }

    public int CacheDurationMinutes { get; set; } = 240;
    
    public int BufferSize { get; set; } = 4096;

    public string FullPath => Path.Combine(FolderPath, FileName);
}

public class MemoryStorageOptions
{
    public const string SectionName = "MemoryStorageOptions";
    
    public int CacheDurationMinutes { get; set; } = 60;
}

