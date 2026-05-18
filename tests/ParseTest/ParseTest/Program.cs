using System;
using System.IO;
using Claunia.PropertyList;

static string? Extract(string path)
{
    var fi = new FileInfo(path);
    var root = PropertyListParser.Parse(fi);
    if (root is NSDictionary dict)
        return dict.ObjectForKey("URL")?.ToString();
    return null;
}

var samples = new[]
{
    @"C:\Users\slohma\repos\private\webloc-opener\tests\sample-xml.webloc",
    @"C:\Users\slohma\repos\private\webloc-opener\tests\sample-binary.webloc",
};

int failures = 0;
foreach (var path in samples)
{
    try
    {
        var url = Extract(path);
        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine($"FAIL  {Path.GetFileName(path)}: no URL");
            failures++;
        }
        else
        {
            Console.WriteLine($"OK    {Path.GetFileName(path)}: {url}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL  {Path.GetFileName(path)}: {ex.Message}");
        failures++;
    }
}

Environment.Exit(failures);
