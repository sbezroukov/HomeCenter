namespace HomeCenter.Services;

public record ImportItem(string Path, string Content);

public record ImportResult(IReadOnlyList<ImportItem> Items, IReadOnlyList<string> Errors);

public interface ITestImportService
{
    ImportResult Parse(string text);
    Task<(IReadOnlyList<string> Created, IReadOnlyList<string> Failed)> CreateFilesAsync(
        IReadOnlyList<ImportItem> items,
        CancellationToken cancellationToken = default);
}
