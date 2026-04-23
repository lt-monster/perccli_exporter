# 🧰 perccli_exporter

[English](README.md) | [简体中文](README.zh-CN.md)

Prometheus exporter for Dell PERC / MegaRAID, built with .NET 10 (Native AOT ready), that exposes RAID health via `perccli64` JSON output.

## ✨ Features
- Exposes `GET /metrics` in Prometheus text format (`version=0.0.4`).
- Polls controller / virtual drive / physical drive data periodically.
- Runs on Windows and Linux.
- Local collection only (SSH/File collectors have been removed).
- Supports port override via command line and environment variables.

## 🏗️ Architecture
![perccli_exporter architecture](architecture.svg)

## ✅ Requirements
- `perccli64` must be installed and available in `PATH`.
- Linux: the exporter needs permission to run `perccli64` (typically via `sudo`).

## ⚙️ Configuration
Default config file: `appsettings.json` (same directory as the executable / current working directory).

```json
{
  "Urls": "http://*:9917",
  "PercOption": {
    "PollingInterval": 3
  }
}
```

- `Urls`: listen address for `/metrics`.
- `PercOption:PollingInterval`: polling interval in seconds.

## 🚀 Run
- Default endpoint: `http://localhost:9917/metrics`

### Windows
Run:

```powershell
.\perccli_exporter.exe
```

Override port:

```powershell
.\perccli_exporter.exe -p 9917
```

Environment variable:

```powershell
$env:PERC_EXPORTER_PORT="9917"
.\perccli_exporter.exe
```

Full URL override:

```powershell
$env:ASPNETCORE_URLS="http://*:9917"
.\perccli_exporter.exe
```

### Linux
Run:

```bash
./perccli_exporter
```

Override port:

```bash
./perccli_exporter -p 9917
```

Environment variable:

```bash
export PERC_EXPORTER_PORT=9917
./perccli_exporter
```

Full URL override:

```bash
export ASPNETCORE_URLS="http://*:9917"
./perccli_exporter
```

### Container (Docker)
Docker Hub: https://hub.docker.com/r/1023785565/perccli_exporter  
Example tag: `1023785565/perccli_exporter:v1.0.0`

```bash
docker pull 1023785565/perccli_exporter:v1.0.0
docker run --rm -p 9917:9917 1023785565/perccli_exporter:v1.0.0
```

Change listen port:

```bash
docker run --rm -p 9999:9999 1023785565/perccli_exporter:v1.0.0 --port 9999
```

Notes:
- The container includes `perccli64` and runs as `root` by default.

### Port override precedence
- Highest: `ASPNETCORE_URLS` / `--urls` (ASP.NET Core standard).
- Then: `--port` / `-p`.
- Then: `PERC_EXPORTER_PORT` / `PORT`.
- Finally: `Urls` in `appsettings.json`.

## 🧱 How It Works
- Runs `perccli64 show J` to discover controllers.
- For each controller, runs `perccli64 /c{ctl}/vall show all J` to collect VD/PD information.
- On Linux, the collector invokes `sudo perccli64 ...`.

## 📈 Metrics
### Controller
| Metric | Labels | Description |
| --- | --- | --- |
| `perc_controller_count` | - | Total RAID controllers |
| `perc_controller_info` | `ctl`, `model` | Controller metadata (value is always 1) |
| `perccli_controller_health_status` | `ctl` | Overall health (1=Optimal, 0=other) |
| `perccli_bbu_status` | `ctl` | BBU status (implementation-specific mapping) |
| `perccli_patrol_read_status` | `ctl` | Scheduled Patrol Read status (implementation-specific mapping) |
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
| `perccli_physical_drive_state` | `ctl`, `vd`, `eid`, `slt` | State (implementation-specific mapping) |
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
