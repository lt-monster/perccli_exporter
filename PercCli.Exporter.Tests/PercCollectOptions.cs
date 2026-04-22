namespace PercCli.Exporter.Tests;

public enum StartMode
{
    File = 0,
    Process = 0x01,
    Ssh = 0x02,
}

public sealed class PercCollectOptions
{
    public StartMode StartMode { get; set; } = StartMode.Process;

    public PercSshConfig? SshConfig { get; set; }

    public int PollingInterval { get; set; } = 3;
}

public sealed class PercSshConfig
{
    public string Host { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
