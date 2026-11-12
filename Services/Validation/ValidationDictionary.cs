namespace Services.Validation;

public interface IValidationDictionary
{
    void AddError(string key, string message);
    bool IsValid { get; }
    IReadOnlyDictionary<string, List<string>> Errors { get; }
    void Clear();
}

public class ValidationDictionary : IValidationDictionary
{
    private readonly Dictionary<string, List<string>> _errors = new();
    public bool IsValid => _errors.Count == 0;
    public IReadOnlyDictionary<string, List<string>> Errors => _errors;
    public void AddError(string key, string message)
    {
        if (!_errors.TryGetValue(key, out var messages))
            _errors[key] = messages = new List<string>();
        messages.Add(message);
    }
    public void Clear() => _errors.Clear();
}
