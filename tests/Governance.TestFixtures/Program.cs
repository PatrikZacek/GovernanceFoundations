using Governance.TestFixtures;

if (args is not ["--hold-lock", var directory]) return 2;
try
{
    using var host = DemoHost.Create(directory);
    await DemoHost.StartAsync(host);
    Console.WriteLine("LOCK_READY");
    await Task.Delay(TimeSpan.FromSeconds(10));
    await DemoHost.StopAsync(host);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}
