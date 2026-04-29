using Storylines.Helpers;
using System.Linq;
using Xunit;

namespace Storylines.Tests.Helpers;

public class RecentProjectDeduplicatorTests
{
    private sealed record FakeRecentProject(string Token, string Path);

    [Fact]
    public void FindExistingToken_Returns_Matching_Token_For_Same_Path()
    {
        var references = new[]
        {
            new RecentProjectReference("alpha", @"C:\Projects\Storylines\Novel.srl"),
            new RecentProjectReference("beta", @"C:\Projects\Storylines\Outline.srl")
        };

        var token = RecentProjectDeduplicator.FindExistingToken(references, @"c:/projects/storylines/novel.srl");

        Assert.Equal("alpha", token);
    }

    [Fact]
    public void DistinctByPath_Removes_Duplicate_Project_Paths()
    {
        var recentProjects = new[]
        {
            new FakeRecentProject("first", @"C:\Stories\Novel.srl"),
            new FakeRecentProject("duplicate", @"c:/stories/novel.srl"),
            new FakeRecentProject("second", @"C:\Stories\Outline.srl")
        };

        var distinctProjects = RecentProjectDeduplicator.DistinctByPath(recentProjects, project => project.Path).ToList();

        Assert.Equal(2, distinctProjects.Count);
        Assert.Equal("first", distinctProjects[0].Token);
        Assert.Equal("second", distinctProjects[1].Token);
    }
}