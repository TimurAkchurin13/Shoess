using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Shoes.Models;

public class Product
{
    public string Article { get; set; } = string.Empty;
    public int ProductNameId { get; set; }
    public int UnitId { get; set; }
    public decimal Price { get; set; }
    public int SupplierId { get; set; }
    public int ManufacturerId { get; set; }
    public int CategoryId { get; set; }
    public decimal? CurrentDiscount { get; set; }
    public int StockQuantity { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }
    
    // Navigation properties
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    
    // Computed properties
    public decimal PriceWithDiscount => Price * (1 - (CurrentDiscount ?? 0) / 100);
    public bool HasHighDiscount => CurrentDiscount > 15;
    
    private const string ResourcePrefix = "avares://Shoes";
    private const string NewImagesFolder = "/Assets/";
    private const string LegacyImagesFolder = "/Views/Images/";
    private const string DefaultImageName = "picture.png";
    private static readonly string[] ExtensionFallbacks = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
    
    // Путь к изображению товара
    private Bitmap? _cachedBitmap;
    
    public string ImagePath
    {
        get
        {
            var normalized = NormalizeImagePath(Photo);
            System.Diagnostics.Debug.WriteLine($"ImagePath: Photo='{Photo ?? "null"}' -> Result='{normalized}'");
            return normalized;
        }
    }
    
    public Bitmap? ImageBitmap
    {
        get
        {
            if (_cachedBitmap != null)
            {
                return _cachedBitmap;
            }
            
            _cachedBitmap = CreateBitmapFromPath(ImagePath);
            return _cachedBitmap;
        }
    }
    
    private static string NormalizeImagePath(string? rawPath)
    {
        var sanitized = SanitizePath(rawPath);
        var candidate = sanitized ?? DefaultImageName;
        
        var diskPath = TryResolveFileSystemPath(candidate);
        if (!string.IsNullOrEmpty(diskPath))
        {
            return diskPath!;
        }
        
        return $"{ResourcePrefix}{NewImagesFolder}{candidate}";
    }
    
    private static string? SanitizePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return DefaultImageName;
        }
        
        var path = rawPath.Trim().Replace("\\", "/");
        
        if (path.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }
        
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        
        if (path.StartsWith("/", StringComparison.Ordinal))
        {
            path = path.TrimStart('/');
        }
        
        if (path.StartsWith("Views/Images/", StringComparison.OrdinalIgnoreCase))
        {
            path = path["Views/Images/".Length..];
        }
        else if (path.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
        {
            path = path["Images/".Length..];
        }
        
        return path;
    }
    
    private static string? TryResolveFileSystemPath(string? sanitizedPath)
    {
        if (string.IsNullOrWhiteSpace(sanitizedPath))
        {
            return null;
        }
        
        if (Path.IsPathRooted(sanitizedPath))
        {
            var resolvedAbsolute = ResolveExistingFile(sanitizedPath);
            return resolvedAbsolute != null ? new Uri(resolvedAbsolute).AbsoluteUri : null;
        }
        
        var relative = sanitizedPath.TrimStart('/');
        foreach (var directory in EnumerateSearchDirectories())
        {
            var candidate = Path.Combine(directory, relative);
            var resolved = ResolveExistingFile(candidate);
            if (resolved != null)
            {
                return new Uri(resolved).AbsoluteUri;
            }
        }
        
        return null;
    }
    
    private static string? ResolveExistingFile(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }
        
        var directory = Path.GetDirectoryName(path);
        var baseName = Path.GetFileNameWithoutExtension(path);
        var originalExt = Path.GetExtension(path);
        
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName))
        {
            return null;
        }
        
        foreach (var ext in BuildExtensionSequence(originalExt))
        {
            var alt = Path.Combine(directory, baseName + ext);
            if (File.Exists(alt))
            {
                return alt;
            }
        }
        
        return null;
    }
    
    private static IEnumerable<string> BuildExtensionSequence(string? originalExt)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (!string.IsNullOrWhiteSpace(originalExt) && seen.Add(originalExt))
        {
            yield return originalExt;
        }
        
        foreach (var fallback in ExtensionFallbacks)
        {
            if (seen.Add(fallback))
            {
                yield return fallback;
            }
        }
    }
    
    private static IEnumerable<string> EnumerateSearchDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dirs = new List<string>();
        
        void TryAdd(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
            
            var normalized = Path.GetFullPath(path);
            if (seen.Add(normalized))
            {
                dirs.Add(normalized);
            }
        }
        
        TryAdd(AppContext.BaseDirectory);
        TryAdd(Path.Combine(AppContext.BaseDirectory, "Images"));
        TryAdd(Directory.GetCurrentDirectory());
        TryAdd(Path.Combine(Directory.GetCurrentDirectory(), "Images"));
        
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
        {
            TryAdd(dir);
            TryAdd(Path.Combine(dir, "Images"));
            
            var parent = Directory.GetParent(dir);
            if (parent == null)
            {
                break;
            }
            dir = parent.FullName;
        }
        
        foreach (var directory in dirs)
        {
            if (Directory.Exists(directory))
            {
                yield return directory;
            }
        }
    }
    
    private static Bitmap? CreateBitmapFromPath(string path)
    {
        try
        {
            var uri = new Uri(path);
            if (uri.IsAbsoluteUri && uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                var localPath = uri.LocalPath;
                if (File.Exists(localPath))
                {
                    return new Bitmap(localPath);
                }
            }
            else if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateBitmapFromPath error for '{path}': {ex.Message}");
        }
        
        try
        {
            var fallbackUri = new Uri($"{ResourcePrefix}{NewImagesFolder}{DefaultImageName}");
            if (AssetLoader.Exists(fallbackUri))
            {
                using var stream = AssetLoader.Open(fallbackUri);
                return new Bitmap(stream);
            }
            
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, "Images", DefaultImageName);
            if (File.Exists(fallbackPath))
            {
                return new Bitmap(fallbackPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fallback bitmap error: {ex.Message}");
        }
        
        return null;
    }
}

