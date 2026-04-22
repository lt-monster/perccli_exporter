# 🧰 perccli_exporter

[English](README.md) | [简体中文](README.zh-CN.md)

Prometheus exporter for Dell PERC / MegaRAID, implemented in .NET 10 and exposing RAID health via `perccli64` JSON output.

## ✨ Features
- Exposes `GET /metrics` in Prometheus text format (`version=0.0.4`).
- Periodically polls controller / virtual drive / physical drive data.
- Works on Windows and Linux.
- Native AOT ready (self-contained publishing enabled in the project file).

## 🏗️ Architecture
![perccli_exporter architecture](architecture.svg)

## ✅ Requirements
- `perccli64` must be installed and available in `PATH`.
- Linux: the exporter needs permission to run `perccli64` (typically via `sudo`).

## 🚀 Run
- Default endpoint: `http://localhost:9917/metrics`
- You can override the listen port via command line or environment variables.

### Command line
Linux:

```bash
./perccli_exporter --port 9917
```

Or use `-p`:

```bash
./perccli_exporter -p 9917
```

Windows:

```powershell
.\perccli_exporter.exe --port 9917
```

Or use `-p`:

```powershell
.\perccli_exporter.exe -p 9917
```

### Environment variables
Supported variables:
- `PERC_EXPORTER_PORT`
- `PORT`

Example (PowerShell):

```powershell
$env:PERC_EXPORTER_PORT="9917"
.\perccli_exporter.exe
```

### Full URL override (ASP.NET Core standard)
If you need to bind to a specific address (or multiple URLs), use `ASPNETCORE_URLS` or `--urls`.
When `ASPNETCORE_URLS`/`--urls` is set, `--port` is ignored.

Linux:

```bash
export ASPNETCORE_URLS="http://*:9917"
./perccli_exporter
```

```powershell
$env:ASPNETCORE_URLS="http://*:9917"
.\perccli_exporter.exe
```

## ⚙️ Configuration
Configuration section: `PercOption`

```json
{
  "Urls": "http://*:9917",
  "PercOption": {
    "PollingInterval": 3
  }
}
```

- `Urls`: Kestrel listen address.
- `PercOption:PollingInterval`: polling interval in seconds.

## 📈 Metrics
### Controller
| Metric | Labels | Description |
| --- | --- | --- |
| `perc_controller_count` | - | Total RAID controllers |
| `perc_controller_info` | `ctl`, `model` | Controller metadata (value is always 1) |
| `perccli_controller_health_status` | `ctl` | Overall health (1=Optimal, 0=other) |
| `perccli_bbu_status` | `ctl` | BBU status (implementation-specific enum mapping) |
| `perccli_patrol_read_status` | `ctl` | Scheduled Patrol Read status (implementation-specific enum mapping) |
| `perccli_emergency_hot_spare_status` | `ctl` | Emergency Hot Spare (EHS) policy (1=enabled, 0=disabled) |
| `perccli_ports_total` | `ctl` | Total physical ports on the controller |
| `perccli_physical_drives_total` | `ctl` | Total physical drives (PD) detected |
| `perccli_physical_drives_degraded_count` | `ctl` | PDs in a non-optimal state |
| `perccli_drive_groups_total` | `ctl` | Total drive groups (DG) |
| `perccli_virtual_drives_total` | `ctl` | Total virtual drives (VD) |
| `perccli_virtual_drives_degraded_count` | `ctl` | VDs in a non-optimal state |
| `perccli_dimmer_switch_status` | `ctl` | Dimmer Switch status (implementation-specific mapping) |
| `perccli_advanced_software_options_count` | `ctl` | Active Advanced Software Options (ASO) count |

### Virtual Drive (VD)
| Metric | Labels | Description |
| --- | --- | --- |
| `perccli_virtual_drive_info` | `ctl`, `dg`, `vd`, `type`, `access`, `cache`, `cac`, `name`, `os_device`, `naa_id` | VD metadata (value is always 1) |
| `perccli_virtual_drive_state` | `ctl`, `dg`, `vd` | State (1=Optl, 2=Degraded, 3=Offline, 0=other) |
| `perccli_virtual_drive_consistent_status` | `ctl`, `dg`, `vd` | Consistency (1=Yes/Consistent, 0=No/other) |
| `perccli_virtual_drive_scc_status` | `ctl`, `dg`, `vd` | Scheduled Consistency Check (1=ON, 0=OFF) |
| `perccli_virtual_drive_size_bytes` | `ctl`, `dg`, `vd` | Size in bytes |
| `perccli_virtual_drive_active_operation` | `ctl`, `dg`, `vd` | Active operation (0=none, 1=background op in progress, 2=unknown) |

### Physical Drive (PD)
| Metric | Labels | Description |
| --- | --- | --- |
| `perccli_physical_drive_info` | `ctl`, `vd`, `eid`, `slt`, `did`, `dg`, `intf`, `med`, `model`, `type`, `sed`, `pi` | PD metadata (value is always 1) |
| `perccli_physical_drive_state` | `ctl`, `vd`, `eid`, `slt` | State (1=Onln, 2=Offln, 3=Rbld, 4=GHS, 5=UGood, 0=other) |
| `perccli_physical_drive_size_bytes` | `ctl`, `vd`, `eid`, `slt` | Size in bytes |
| `perccli_physical_drive_sector_size_bytes` | `ctl`, `vd`, `eid`, `slt` | Sector size in bytes |
| `perccli_physical_drive_sp_status` | `ctl`, `vd`, `eid`, `slt` | Spare status (0=not spare, 1=HS dedicated, 2=PS global) |

## 🔐 Linux sudoers example
If you want to run the exporter as a non-root user, grant `perccli64` passwordless sudo:

```bash
sudo tee /etc/sudoers.d/perccli64 >/dev/null <<'EOF'
your_user ALL=(root) NOPASSWD: /usr/sbin/perccli64
EOF
sudo chmod 0440 /etc/sudoers.d/perccli64
```

## 📄 License
MIT License, see [LICENSE](LICENSE).
