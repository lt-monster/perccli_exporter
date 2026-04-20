using System.Buffers;
using System.Buffers.Text;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using PercCli.Exporter.Stores;

namespace PercCli.Exporter;

public sealed class PercMetricWriter(PercMetricStore metricStore)
{
    public void WriteControllerMetrics(PipeWriter writer)
    {
        var currentCtls = metricStore.Current.ControllerMetricStore;

        if (currentCtls.Count == 0)
        {
            writer.Write("# HELP perc_controller_count Total RAID controllers\n"u8);
            writer.Write("# TYPE perc_controller_count gauge\n"u8);
            writer.Write("perc_controller_count 0\n"u8);
            writer.Write("# No controllers found\n"u8);
            return;
        }
        
        writer.Write("# HELP perc_controller_count Total RAID controllers\n"u8);
        writer.Write("# TYPE perc_controller_count gauge\n"u8);
        writer.Write("perc_controller_count "u8);
        writer.WriteInt(currentCtls.Count);
        writer.Write("\n"u8);

        foreach (var ctl in currentCtls)
        {
            if (ctl is null) continue;

            writer.Write("# HELP perccli_controller_info Metadata about the controller, such as the hardware model. Value is always 1.\n"u8);
            writer.Write("# TYPE perccli_controller_info gauge\n"u8);
            writer.Write("perc_controller_info{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\",model=\""u8);
            writer.Write(StringStore.GetBytes(ctl.Model));
            writer.Write("\"} 1"u8);
            writer.Write("\n"u8);

            // 1: 最佳状态, 0: 只要不是 Opt，一律视为异常
            writer.Write("# HELP perccli_controller_health_status Overall health of the controller. 1 for Optimal, 0 for any other state.\n"u8);
            writer.Write("# TYPE perccli_controller_health_status gauge\n"u8);
            writer.Write("perccli_controller_health_status{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.Hlth);
            writer.Write("\n"u8);

            // 1: Optimal, 2: Maintenance/Transient, -1: Not Present, 0: Failure/Warning/...other
            writer.Write("# HELP perccli_bbu_status Status of the Battery Backup Unit (BBU). 1 for Optimal, 0 for Bad or Missing.\n"u8);
            writer.Write("# TYPE perccli_bbu_status gauge\n"u8);
            writer.Write("perccli_bbu_status{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.Bbu);
            writer.Write("\n"u8);

            // 1: On, 2: Active, 0: Off(Default), 3: Suspended
            writer.Write("# HELP perccli_patrol_read_status Status of the Scheduled Patrol Read. 1 for On, 0 for Off.\n"u8);
            writer.Write("# TYPE perccli_patrol_read_status gauge\n"u8);
            writer.Write("perccli_patrol_read_status{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.Spr);
            writer.Write("\n"u8);

            // 1: Y/Yes/On, 0: N/No/Off
            writer.Write("# HELP perccli_emergency_hot_spare_status Status of the Emergency Hot Spare (EHS) policy. 1 for enabled, 0 for disabled.\n"u8);
            writer.Write("# TYPE perccli_emergency_hot_spare_status gauge\n"u8);
            writer.Write("perccli_emergency_hot_spare_status{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.Ehs);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_ports_total Total number of physical ports available on the controller.\n"u8);
            writer.Write("# TYPE perccli_ports_total gauge\n"u8);
            writer.Write("perccli_ports_total{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.Ports);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_physical_drives_total Total number of physical drives (PD) detected by the controller.\n"u8);
            writer.Write("# TYPE perccli_physical_drives_total gauge\n"u8);
            writer.Write("perccli_physical_drives_total{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.PDs);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_physical_drives_degraded_count Number of physical drives in a non-optimal state.\n"u8);
            writer.Write("# TYPE perccli_physical_drives_degraded_count gauge\n"u8);
            writer.Write("perccli_physical_drives_degraded_count{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.DnOpt);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_drive_groups_total Total number of configured Drive Groups (DG).\n"u8);
            writer.Write("# TYPE perccli_drive_groups_total gauge\n"u8);
            writer.Write("perccli_drive_groups_total{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.DGs);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_virtual_drives_total Total number of configured Virtual Drives (VD/RAID volumes).\n"u8);
            writer.Write("# TYPE perccli_virtual_drives_total gauge\n"u8);
            writer.Write("perccli_virtual_drives_total{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.VDs);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_virtual_drives_degraded_count Number of virtual drives in a non-optimal state.\n"u8);
            writer.Write("# TYPE perccli_virtual_drives_degraded_count gauge\n"u8);
            writer.Write("perccli_virtual_drives_degraded_count{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.VnOpt);
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_dimmer_switch_status Current state of the power-saving Dimmer Switch setting. 1 for On, 0 otherwise.\n"u8);
            writer.Write("# TYPE perccli_dimmer_switch_status gauge\n"u8);
            writer.Write("perccli_dimmer_switch_status{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.Write(StringStore.GetBytes(ctl.Ds));
            writer.Write("\n"u8);

            writer.Write("# HELP perccli_advanced_software_options_count Number of active Advanced Software Options (ASO) on the controller.\n"u8);
            writer.Write("# TYPE perccli_advanced_software_options_count gauge\n"u8);
            writer.Write("perccli_advanced_software_options_count{ctl=\""u8);
            writer.WriteInt(ctl.Ctl);
            writer.Write("\"} "u8);
            writer.WriteInt(ctl.AsOs);
            writer.Write("\n"u8);
        }
    }
    
    public void WriteVirtualDriveMetrics(PipeWriter writer)
    {
        var currentVds = metricStore.Current.VirtualDriveMetricStore;
        
        if (currentVds.Count == 0) return;
        
        var stateBuffer = new ArrayBufferWriter<byte>(1024);
        var consistBuffer = new ArrayBufferWriter<byte>(1024);
        var sccBuffer = new ArrayBufferWriter<byte>(1024);
        var sizeBuffer = new ArrayBufferWriter<byte>(1024);
        var activeOperationsBuffer = new ArrayBufferWriter<byte>(1024);

        writer.Write("# HELP perccli_virtual_drive_info Metadata about the virtual drive.\n"u8);
        writer.Write("# TYPE perccli_virtual_drive_info gauge\n"u8);

        stateBuffer.Write("# HELP perccli_virtual_drive_state State of the virtual drive (1 for Optl, 0 otherwise).\n"u8);
        stateBuffer.Write("# TYPE perccli_virtual_drive_state gauge\n"u8);

        consistBuffer.Write("# HELP perccli_virtual_drive_consistent_status Indicates if the virtual drive data is consistent (1 for Yes/Consistent, 0 for No/Inconsistent).\n"u8);
        consistBuffer.Write("# TYPE perccli_virtual_drive_consistent_status gauge\n"u8);

        sccBuffer.Write("# HELP perccli_virtual_drive_scc_status Indicates the status of the Scheduled Consistency Check (sCC) (1 for ON, 0 for OFF).\n"u8);
        sccBuffer.Write("# TYPE perccli_virtual_drive_scc_status gauge\n"u8);

        sizeBuffer.Write("# HELP perccli_virtual_drive_size_bytes Size of the virtual drive in bytes.\n"u8);
        sizeBuffer.Write("# TYPE perccli_virtual_drive_size_bytes gauge\n"u8);
        
        activeOperationsBuffer.Write("# HELP perccli_virtual_drive_active_operation RAID controller virtual drive active operation status. 0 = No active operation (normal), 1 = Background operation in progress (rebuild/check/init/migrate/expand/patrol read), 2 = Unknown operation.\n"u8);
        activeOperationsBuffer.Write("# TYPE perccli_virtual_drive_active_operation gauge\n"u8);

        foreach (var vd in currentVds)
        {
            if (vd is null) continue;

            writer.Write("perccli_virtual_drive_info{ctl=\""u8);
            writer.WriteInt(vd.CtlId);
            writer.Write("\",dg=\""u8);
            writer.WriteInt(vd.Dg);
            writer.Write("\",vd=\""u8);
            writer.WriteInt(vd.Vd);
            writer.Write("\",type=\""u8);
            writer.Write(StringStore.GetBytes(vd.Type));
            writer.Write("\",access=\""u8);
            writer.Write(StringStore.GetBytes(vd.Access));
            writer.Write("\",cache=\""u8);
            writer.Write(StringStore.GetBytes(vd.Cache));
            writer.Write("\",cac=\""u8);
            writer.Write(StringStore.GetBytes(vd.Cac));
            writer.Write("\",name=\""u8);
            writer.Write(StringStore.GetBytes(vd.Name));
            writer.Write("\",os_device=\""u8);
            writer.Write(StringStore.GetBytes(vd.OsDevice));
            writer.Write("\",naa_id=\""u8);
            writer.Write(StringStore.GetBytes(vd.NaaId));
            writer.Write("\"} 1"u8);
            writer.Write("\n"u8);

            //Optl (Optimal) = 1, Degraded = 2, Offline = 3, 其他 = 0
            stateBuffer.Write("perccli_virtual_drive_state{ctl=\""u8);
            stateBuffer.WriteInt(vd.CtlId);
            stateBuffer.Write("\",dg=\""u8);
            stateBuffer.WriteInt(vd.Dg);
            stateBuffer.Write("\",vd=\""u8);
            stateBuffer.WriteInt(vd.Vd);
            stateBuffer.Write("\"} "u8);
            stateBuffer.WriteInt(vd.State);
            stateBuffer.Write("\n"u8);

            //Yes = 1, No/其他 = 0
            consistBuffer.Write("perccli_virtual_drive_consistent_status{ctl=\""u8);
            consistBuffer.WriteInt(vd.CtlId);
            consistBuffer.Write("\",dg=\""u8);
            consistBuffer.WriteInt(vd.Dg);
            consistBuffer.Write("\",vd=\""u8);
            consistBuffer.WriteInt(vd.Vd);
            consistBuffer.Write("\"} "u8);
            consistBuffer.WriteInt(vd.Consist);
            consistBuffer.Write("\n"u8);
            
            //ON=1, OFF=0
            sccBuffer.Write("perccli_virtual_drive_scc_status{ctl=\""u8);
            sccBuffer.WriteInt(vd.CtlId);
            sccBuffer.Write("\",dg=\""u8);
            sccBuffer.WriteInt(vd.Dg);
            sccBuffer.Write("\",vd=\""u8);
            sccBuffer.WriteInt(vd.Vd);
            sccBuffer.Write("\"} "u8);
            sccBuffer.WriteInt(vd.Scc);
            sccBuffer.Write("\n"u8);

            sizeBuffer.Write("perccli_virtual_drive_size_bytes{ctl=\""u8);
            sizeBuffer.WriteInt(vd.CtlId);
            sizeBuffer.Write("\",dg=\""u8);
            sizeBuffer.WriteInt(vd.Dg);
            sizeBuffer.Write("\",vd=\""u8);
            sizeBuffer.WriteInt(vd.Vd);
            sizeBuffer.Write("\"} "u8);
            sizeBuffer.WriteDouble(vd.SizeBytes);
            sizeBuffer.Write("\n"u8);

            activeOperationsBuffer.Write("perccli_virtual_drive_active_operation{ctl=\""u8);
            activeOperationsBuffer.WriteInt(vd.CtlId);
            activeOperationsBuffer.Write("\",dg=\""u8);
            activeOperationsBuffer.WriteInt(vd.Dg);
            activeOperationsBuffer.Write("\",vd=\""u8);
            activeOperationsBuffer.WriteInt(vd.Vd);
            activeOperationsBuffer.Write("\"} "u8);
            activeOperationsBuffer.WriteDouble(vd.ActiveOperations);
            activeOperationsBuffer.Write("\n"u8);
        }

        writer.Write(stateBuffer.WrittenSpan);
        writer.Write(consistBuffer.WrittenSpan);
        writer.Write(sccBuffer.WrittenSpan);
        writer.Write(sizeBuffer.WrittenSpan);
    }

    public void WritePhysicalDriveMetrics(PipeWriter writer)
    {
        var currentPds = metricStore.Current.PhysicalDriveMetricStore;

        if (currentPds.Count == 0) return;

        var stateBuffer = new ArrayBufferWriter<byte>(1024);
        var sizeBuffer = new ArrayBufferWriter<byte>(1024);
        var seSzBuffer = new ArrayBufferWriter<byte>(1024);
        var spBuffer = new ArrayBufferWriter<byte>(1024);

        writer.Write("# HELP perccli_physical_drive_info Metadata about the physical drive.\n"u8);
        writer.Write("# TYPE perccli_physical_drive_info gauge\n"u8);

        stateBuffer.Write("# HELP perccli_physical_drive_state State of the physical drive (1=Onln, 2=Offln, 3=Rbld, 4=GHS, 5=UGood, 0=other).\n"u8);
        stateBuffer.Write("# TYPE perccli_physical_drive_state gauge\n"u8);

        sizeBuffer.Write("# HELP perccli_physical_drive_size_bytes Size of the physical drive in bytes.\n"u8);
        sizeBuffer.Write("# TYPE perccli_physical_drive_size_bytes gauge\n"u8);

        seSzBuffer.Write("# HELP perccli_physical_drive_sector_size_bytes Sector size of the physical drive in bytes.\n"u8);
        seSzBuffer.Write("# TYPE perccli_physical_drive_sector_size_bytes gauge\n"u8);

        spBuffer.Write("# HELP perccli_physical_drive_sp_status Spare status of the physical drive (0=not spare, 1=HS dedicated hot spare, 2=PS global hot spare).\n"u8);
        spBuffer.Write("# TYPE perccli_physical_drive_sp_status gauge\n"u8);

        foreach (var pd in currentPds)
        {
            if (pd is null) continue;

            writer.Write("perccli_physical_drive_info{ctl=\""u8);
            writer.WriteInt(pd.CtlId);
            writer.Write("\",vd=\""u8);
            writer.WriteInt(pd.Vd);
            writer.Write("\",eid=\""u8);
            writer.WriteInt(pd.Eid);
            writer.Write("\",slt=\""u8);
            writer.WriteInt(pd.Slt);
            writer.Write("\",did=\""u8);
            writer.WriteInt(pd.Did);
            writer.Write("\",dg=\""u8);
            writer.WriteInt(pd.Dg);
            writer.Write("\",intf=\""u8);
            writer.Write(StringStore.GetBytes(pd.Intf));
            writer.Write("\",med=\""u8);
            writer.Write(StringStore.GetBytes(pd.Med));
            writer.Write("\",model=\""u8);
            writer.Write(StringStore.GetBytes(pd.Model));
            writer.Write("\",type=\""u8);
            writer.Write(StringStore.GetBytes(pd.Type));
            writer.Write("\",sed=\""u8);
            writer.WriteInt(pd.Sed);
            writer.Write("\",pi=\""u8);
            writer.WriteInt(pd.Pi);
            writer.Write("\"} 1\n"u8);

            stateBuffer.Write("perccli_physical_drive_state{ctl=\""u8);
            stateBuffer.WriteInt(pd.CtlId);
            stateBuffer.Write("\",vd=\""u8);
            stateBuffer.WriteInt(pd.Vd);
            stateBuffer.Write("\",eid=\""u8);
            stateBuffer.WriteInt(pd.Eid);
            stateBuffer.Write("\",slt=\""u8);
            stateBuffer.WriteInt(pd.Slt);
            stateBuffer.Write("\"} "u8);
            stateBuffer.WriteInt(pd.State);
            stateBuffer.Write("\n"u8);

            sizeBuffer.Write("perccli_physical_drive_size_bytes{ctl=\""u8);
            sizeBuffer.WriteInt(pd.CtlId);
            sizeBuffer.Write("\",vd=\""u8);
            sizeBuffer.WriteInt(pd.Vd);
            sizeBuffer.Write("\",eid=\""u8);
            sizeBuffer.WriteInt(pd.Eid);
            sizeBuffer.Write("\",slt=\""u8);
            sizeBuffer.WriteInt(pd.Slt);
            sizeBuffer.Write("\"} "u8);
            sizeBuffer.WriteDouble(pd.SizeBytes);
            sizeBuffer.Write("\n"u8);

            seSzBuffer.Write("perccli_physical_drive_sector_size_bytes{ctl=\""u8);
            seSzBuffer.WriteInt(pd.CtlId);
            seSzBuffer.Write("\",vd=\""u8);
            seSzBuffer.WriteInt(pd.Vd);
            seSzBuffer.Write("\",eid=\""u8);
            seSzBuffer.WriteInt(pd.Eid);
            seSzBuffer.Write("\",slt=\""u8);
            seSzBuffer.WriteInt(pd.Slt);
            seSzBuffer.Write("\"} "u8);
            seSzBuffer.WriteDouble(pd.SeSz);
            seSzBuffer.Write("\n"u8);

            // 0=未配置热备, 1=专用热备(HS), 2=全局热备(PS)
            spBuffer.Write("perccli_physical_drive_sp_status{ctl=\""u8);
            spBuffer.WriteInt(pd.CtlId);
            spBuffer.Write("\",vd=\""u8);
            spBuffer.WriteInt(pd.Vd);
            spBuffer.Write("\",eid=\""u8);
            spBuffer.WriteInt(pd.Eid);
            spBuffer.Write("\",slt=\""u8);
            spBuffer.WriteInt(pd.Slt);
            spBuffer.Write("\"} "u8);
            spBuffer.WriteInt(pd.Sp);
            spBuffer.Write("\n"u8);
        }

        writer.Write(stateBuffer.WrittenSpan);
        writer.Write(sizeBuffer.WrittenSpan);
        writer.Write(seSzBuffer.WrittenSpan);
        writer.Write(spBuffer.WrittenSpan);
    }
}

public static class BufferWriterExtensions
{
    extension(IBufferWriter<byte> writer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt(int value)
        {
            var span = writer.GetSpan(11);
            if (Utf8Formatter.TryFormat(value, span, out var bytesWritten))
            {
                writer.Advance(bytesWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            var span = writer.GetSpan(32);
            if (Utf8Formatter.TryFormat(value, span, out var bytesWritten))
            {
                writer.Advance(bytesWritten);
            }
        }
    }
}