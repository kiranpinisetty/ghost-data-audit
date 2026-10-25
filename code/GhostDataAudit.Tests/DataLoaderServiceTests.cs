using GhostDataAudit.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GhostDataAudit.Tests;

public class DataLoaderServiceTests
{
    private readonly DataLoaderService _loader;

    public DataLoaderServiceTests()
    {
        _loader = new DataLoaderService(NullLogger<DataLoaderService>.Instance);
    }

    [Fact]
    public async Task LoadDeletionRequests_ValidFixture_ParsesProperly()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "case_id,email,requested_at,reported_status,reported_at,audit_at,request_text\n" +
                          "case_01,user@test.com,2026-08-18T09:14:00Z,COMPLETE,2026-08-20T16:42:00Z,2026-09-03T11:30:00Z,\"Please delete my data, thank you.\"\n";
            await File.WriteAllTextAsync(tempFile, content);

            var list = await _loader.LoadDeletionRequestsAsync(tempFile);
            Assert.Single(list);
            Assert.Equal("case_01", list[0].CaseId);
            Assert.Equal("user@test.com", list[0].Email);
            Assert.Equal("COMPLETE", list[0].ReportedStatus);
            Assert.Equal("Please delete my data, thank you.", list[0].RequestText);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDeletionRequests_DuplicateCaseId_ThrowsInvalidDataException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "case_id,email,requested_at,reported_status,reported_at,audit_at,request_text\n" +
                          "case_01,user1@test.com,2026-08-18T09:14:00Z,COMPLETE,2026-08-20T16:42:00Z,2026-09-03T11:30:00Z,text\n" +
                          "case_01,user2@test.com,2026-08-18T09:14:00Z,COMPLETE,2026-08-20T16:42:00Z,2026-09-03T11:30:00Z,text\n";
            await File.WriteAllTextAsync(tempFile, content);

            await Assert.ThrowsAsync<InvalidDataException>(() => _loader.LoadDeletionRequestsAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDeletionRequests_MissingColumnHeader_ThrowsInvalidDataException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "case_id,email,wrong_header\ncase_01,user@test.com,wrong\n";
            await File.WriteAllTextAsync(tempFile, content);

            await Assert.ThrowsAsync<InvalidDataException>(() => _loader.LoadDeletionRequestsAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResolveSafeEvidencePath_ValidRelativePath_ResolvesWithinDirectory()
    {
        var baseDir = Path.GetTempPath();
        var safePath = _loader.ResolveSafeEvidencePath(baseDir, "evidence/logs/test.log");

        Assert.StartsWith(Path.GetFullPath(baseDir), safePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("test.log", safePath);
    }

    [Fact]
    public void ResolveSafeEvidencePath_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "test_sandbox");
        Directory.CreateDirectory(baseDir);
        try
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                _loader.ResolveSafeEvidencePath(baseDir, "../../etc/passwd"));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }
}

