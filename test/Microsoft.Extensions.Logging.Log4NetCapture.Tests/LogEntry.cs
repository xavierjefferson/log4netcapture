using Newtonsoft.Json;

namespace Snork.Log4NetCapture.Tests;

public class LogEntry
{
    [JsonIgnore]
    public Exception? EventualPassedException { get; set; }
    public string? Scope { get; set; }
    public DateTime Date { get; set; }
    public string Level { get; set; }
    public string Logger { get; set; }
    public string Thread { get; set; }
    public string? NDC { get; set; }
    public string? Message { get; set; }
}