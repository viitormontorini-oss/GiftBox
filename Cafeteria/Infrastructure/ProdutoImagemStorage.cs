namespace Cafeteria.Infrastructure;

public static class ProdutoImagemStorage
{
    public static async Task<string?> SalvarAsync(IFormFile? foto, IWebHostEnvironment environment)
    {
        if (foto == null || foto.Length == 0) return null;
        if (foto.Length > 5 * 1024 * 1024) throw new InvalidOperationException("A foto deve ter até 5 MB.");
        using var memory = new MemoryStream();
        await foto.CopyToAsync(memory);
        var data = memory.ToArray();
        string? extension = data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : data.Length >= 3 && data[0] == 255 && data[1] == 216 && data[2] == 255 ? ".jpg"
            : data.Length >= 12 && System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(data, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension == null) throw new InvalidOperationException("Envie uma imagem PNG, JPG ou WebP.");
        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "produtos");
        Directory.CreateDirectory(folder);
        var name = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(folder, name), data);
        return "/uploads/produtos/" + name;
    }

    public static void Remover(string? url, IWebHostEnvironment environment)
    {
        if (url == null || !System.Text.RegularExpressions.Regex.IsMatch(url, "^/uploads/produtos/[a-f0-9]{32}\\.(png|jpg|webp)$")) return;
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var path = Path.Combine(root, "uploads", "produtos", Path.GetFileName(url));
        if (File.Exists(path)) File.Delete(path);
    }
}
