using PercCli.Exporter.Stores;
using Renci.SshNet;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace PercCli.Exporter.Collectors;

//通过SSH方式采集
public sealed class SshPercCollector(PercCollectOptions collOpts, PercMetricStore metricStore): PercCollector(metricStore)
{
    private readonly SshClient sshClient = new(new ConnectionInfo(collOpts.SshConfig!.Host, collOpts.SshConfig!.Username, 
        new PasswordAuthenticationMethod(collOpts.SshConfig!.Username, collOpts.SshConfig!.Password))
    {
        Timeout = TimeSpan.FromSeconds(3)
    });
    
    public override async Task CollectControllerMetrics(CancellationToken stoppingToken)
    {
        try
        {
            if (!sshClient.IsConnected)
            {
                await sshClient.ConnectAsync(stoppingToken);
            }
            
            using var sshCommand = sshClient.CreateCommand(CMD_CONTROLLERS);
            var asyncResult = sshCommand.BeginExecute();

            await HandingControllers(sshCommand.OutputStream, stoppingToken);
            
            sshCommand.EndExecute(asyncResult);
            if (sshCommand.ExitStatus != 0)
            {
                var error = sshCommand.Error;
                Console.WriteLine($"Controller query failed, cause by：{error}");
            }
        }
        catch (Exception ex)
        {
            
        }
    }

    public override async Task CollectVirtualDriveMetrics(CancellationToken stoppingToken)
    {
        try
        {
            if (!sshClient.IsConnected)
            {
                await sshClient.ConnectAsync(stoppingToken);
            }

            foreach (var ctl in MetricStore.Snapshot.ControllerMetricStore)
            {
                if (ctl is null) continue;

                var cmd = GetVirtualDriveQueryCmd(ctl.Ctl);
                using var sshCommand = sshClient.CreateCommand(cmd);
                var asyncResult = sshCommand.BeginExecute();
                
                await HandingVirtualDrives(sshCommand.OutputStream, ctl.Ctl, stoppingToken);
                
                sshCommand.EndExecute(asyncResult);
                if (sshCommand.ExitStatus != 0)
                {
                    var error = sshCommand.Error;
                    Console.WriteLine($"Controller query failed, cause by：{error}");
                }
            }
        }
        catch (Exception e)
        {
            
        }
            
    }
}