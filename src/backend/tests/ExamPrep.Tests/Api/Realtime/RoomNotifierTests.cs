using ExamPrep.Api.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ExamPrep.Tests.Api.Realtime;

/// <summary>A section whose message could not be sent stays marked as changed, so the next flush sends it.</summary>
public sealed class RoomNotifierTests
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed class Proxy(Func<Task> send) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => send();
    }

    private sealed class FakeClients(IClientProxy group) : IHubClients
    {
        public IClientProxy All => group;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => group;

        public IClientProxy Client(string connectionId) => group;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => group;

        public IClientProxy Group(string groupName) => group;

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => group;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => group;

        public IClientProxy User(string userId) => group;

        public IClientProxy Users(IReadOnlyList<string> userIds) => group;
    }

    private sealed class Context<THub>(IClientProxy group) : IHubContext<THub>
        where THub : Hub
    {
        public IHubClients Clients { get; } = new FakeClients(group);

        public IGroupManager Groups => throw new NotSupportedException();
    }

    [Fact]
    public async Task AFailedSend_KeepsTheSectionMarkedSoTheNextFlushSendsIt()
    {
        var calls = 0;
        var failFirst = true;
        var proxy = new Proxy(() =>
        {
            calls++;
            if (failFirst)
            {
                failFirst = false;
                throw new InvalidOperationException("connection lost");
            }

            return Task.CompletedTask;
        });
        var notifier = new RoomNotifier(new Context<StaffRoomHub>(proxy), new Context<CandidateRoomHub>(proxy));
        notifier.SectionChanged(7);
        notifier.SectionChanged(9);

        await Assert.ThrowsAsync<InvalidOperationException>(() => notifier.FlushAsync(ct));
        await notifier.FlushAsync(ct);

        // One attempt that failed, then both sections on the second flush.
        Assert.Equal(3, calls);
    }
}
