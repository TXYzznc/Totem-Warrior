using System;
using System.Collections.Generic;
using System.IO;

public enum GFDiagnosticScenarioMode
{
    EditMode,
    PlayMode,
    Any,
}

public interface IGFDiagnosticScenario
{
    string Name { get; }
    string Category { get; }
    GFDiagnosticScenarioMode Mode { get; }
    void Run(GFDiagnosticScenarioContext context);
}

public abstract class GFDiagnosticScenarioBase : IGFDiagnosticScenario
{
    public virtual string Name => GetType().Name;
    public virtual string Category => "General";
    public virtual GFDiagnosticScenarioMode Mode => GFDiagnosticScenarioMode.EditMode;
    public abstract void Run(GFDiagnosticScenarioContext context);
}

public sealed class GFDiagnosticScenarioContext
{
    private readonly string system;

    public GFDiagnosticScenarioContext(GFDiagnosticReportItem item, string scenarioName, string category)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        ScenarioName = string.IsNullOrWhiteSpace(scenarioName) ? "Unnamed Scenario" : scenarioName;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
        system = $"DiagnosticScenario.{Category}";
    }

    public GFDiagnosticReportItem Item { get; }

    public string ScenarioName { get; }

    public string Category { get; }

    public void Pass(string message = null)
    {
        Item.Pass(message);
    }

    public void Fail(string message)
    {
        Item.Fail(message);
        TraceFailure("Assert.Failed", message);
    }

    public void Warn(string message)
    {
        Item.Warn(message);
        TraceWarning("Assert.Warning", message);
    }

    public void Detail(string key, object value)
    {
        Item.Detail(key, FormatValue(value));
    }

    public void TraceInfo(string action, string message = null, params string[] data)
    {
        GFTrace.Info(system, BuildAction(action), message, GFTrace.Data(data));
    }

    public void TraceSuccess(string action, string message = null, params string[] data)
    {
        GFTrace.Success(system, BuildAction(action), message, GFTrace.Data(data));
    }

    public void TraceWarning(string action, string message = null, params string[] data)
    {
        GFTrace.Warning(system, BuildAction(action), message, GFTrace.Data(data));
    }

    public void TraceFailure(string action, string message = null, params string[] data)
    {
        GFTrace.Failure(system, BuildAction(action), message, GFTrace.Data(data));
    }

    public void Assert(bool condition, string failMessage, string successMessage = null)
    {
        if (!condition)
        {
            Fail(failMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(successMessage))
        {
            TraceSuccess("Assert.Passed", successMessage);
        }
    }

    public void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Fail($"{name}: expected={FormatValue(expected)}, actual={FormatValue(actual)}");
            return;
        }

        Detail($"{name}.actual", actual);
    }

    public void RequireFile(string fileName, string message = null)
    {
        Assert(File.Exists(fileName), message ?? $"File does not exist: {fileName}");
    }

    public void RequireDirectory(string directoryName, string message = null)
    {
        Assert(Directory.Exists(directoryName), message ?? $"Directory does not exist: {directoryName}");
    }

    private string BuildAction(string action)
    {
        return string.IsNullOrWhiteSpace(action) ? ScenarioName : $"{ScenarioName}.{action}";
    }

    private static string FormatValue(object value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        try
        {
            Type valueType = value.GetType();
            if (!valueType.IsPrimitive && !valueType.IsEnum && valueType != typeof(decimal))
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(value);
            }
        }
        catch
        {
            // Fall back to ToString below.
        }

        return value.ToString();
    }
}
