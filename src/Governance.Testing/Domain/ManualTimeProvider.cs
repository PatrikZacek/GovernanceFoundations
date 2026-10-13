namespace Governance.Testing.Domain;

public sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private long ticks = initial.UtcTicks;
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref ticks), TimeSpan.Zero);
    public void Advance(TimeSpan amount) => Interlocked.Add(ref ticks, amount.Ticks);
}

