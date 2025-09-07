using Microsoft.EntityFrameworkCore;
using ProfileBookAPI.Data;
using ProfileBookAPI.Models;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace ProfileBookAPI.Tests
{
    public class PostRepositoryTests
    {
        private DbContextOptions<AppDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "posts_repo_test_" + System.Guid.NewGuid())
                .Options;
        }

        [Fact]
        public async Task AddAndGetPost_Works()
        {
            var options = CreateOptions();

            using (var ctx = new AppDbContext(options))
            {
                ctx.Users.Add(new User { Id = 1, Username = "u1", PasswordHash = "x" });
                ctx.Posts.Add(new Post { Id = 1, Content = "c1", Status = "Approved", UserId = 1 });
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AppDbContext(options))
            {
                var posts = await ctx.Posts.ToListAsync();
                posts.Should().HaveCount(1);
                posts[0].Content.Should().Be("c1");
            }
        }
    }
}
