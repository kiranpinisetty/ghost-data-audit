using GhostDataAudit.Api.Data;
using Xunit;

namespace GhostDataAudit.Tests;

public class CsvReaderUtilityTests
{
    [Fact]
    public void Parse_SimpleCsv_ParsesCorrectly()
    {
        var csv = "col1,col2,col3\nval1,val2,val3\nval4,val5,val6";
        var result = CsvReaderUtility.Parse(csv);

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { "col1", "col2", "col3" }, result[0]);
        Assert.Equal(new[] { "val1", "val2", "val3" }, result[1]);
        Assert.Equal(new[] { "val4", "val5", "val6" }, result[2]);
    }

    [Fact]
    public void Parse_QuotedCommasAndEscapedQuotes_ParsesCorrectly()
    {
        var csv = "id,text\n1,\"Hello, world!\"\n2,\"He said \"\"delete everything\"\" right now.\"";
        var result = CsvReaderUtility.Parse(csv);

        Assert.Equal(3, result.Count);
        Assert.Equal("Hello, world!", result[1][1]);
        Assert.Equal("He said \"delete everything\" right now.", result[2][1]);
    }

    [Fact]
    public void Parse_WindowsCRLF_HandlesCorrectly()
    {
        var csv = "col1,col2\r\nv1,v2\r\nv3,v4\r\n";
        var result = CsvReaderUtility.Parse(csv);

        Assert.Equal(3, result.Count);
        Assert.Equal("v1", result[1][0]);
        Assert.Equal("v4", result[2][1]);
    }
}

