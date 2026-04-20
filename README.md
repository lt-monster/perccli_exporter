# 🧰 perccli_exporter

[English](README.md) | [简体中文](README.zh-CN.md)

## 📌 Overview
- A lightweight Prometheus exporter that exposes Dell PERC / MegaRAID status by parsing `perccli64` JSON output.
- Provides `/metrics` for Prometheus scraping (text format `version=0.0.4`).

## 🏗️ Architecture
![perccli_exporter architecture](architecture.svg)

## ✨ Features
- Controller-level metrics (count, model metadata, health, BBU, patrol read, ports, PD/VD stats).
- Virtual drive (VD) metrics (metadata, state, consistency, scheduled CC, size).
- Physical drive (PD) metrics (metadata, state, size, sector size, hot spare status).
- Background polling with a configurable interval.

## 🧱 How It Works
- A background service polls RAID data on an interval.
- Collected data is stored in-memory and exposed as Prometheus metrics at `GET /metrics`.
- Data sources (StartMode):
  - `File`: reads sample JSON from `PercCli.Exporter/Data/*.json` (useful for development/demo).
  - `Ssh`: runs `sudo perccli64 ... show J` on a remote host via SSH and parses JSON.
  - `Process`: currently not implemented (kept as a placeholder).

## ⚙️ Configuration
Configuration key: `PercOption`

```json
{
  "PercOption": {
    "StartMode": 2,
    "SshConfig": {
      "Host": "192.0.2.10",
      "Username": "raid",
      "Password": "change-me"
    },
    "PollingInterval": 3
  }
}
```

- `StartMode`: `0=File`, `1=Process`, `2=Ssh`
- `SshConfig`: used by `Ssh` mode
- `PollingInterval`: polling interval in seconds

## 🚀 Quick Start
### 🧪 Run (development)

```bash
dotnet run --project .\PercCli.Exporter\PercCli.Exporter.csproj
```

Then open:
- `http://localhost:5000/metrics` (or the port shown in console)

### 🏗️ Publish (native AOT)
This project enables native AOT + trimming in the project file.

```bash
dotnet publish .\PercCli.Exporter\PercCli.Exporter.csproj -c Release -o .\out
```

## 📈 Metrics
### 🎛️ Controller metrics
| Metric | Labels | Description |
| --- | --- | --- |
| `perc_controller_count` | - | Total RAID controllers |
| `perc_controller_info` | `ctl`, `model` | Controller metadata (value is always 1) |
| `perccli_controller_health_status` | `ctl` | Overall health (1=Optimal, 0=other) |
| `perccli_bbu_status` | `ctl` | Battery Backup Unit status (1=Optimal, 0=bad or missing) |
| `perccli_patrol_read_status` | `ctl` | Scheduled Patrol Read status (1=On, 0=Off) |
| `perccli_emergency_hot_spare_status` | `ctl` | Emergency Hot Spare (EHS) policy (1=enabled, 0=disabled) |
| `perccli_ports_total` | `ctl` | Total physical ports on the controller |
| `perccli_physical_drives_total` | `ctl` | Total physical drives (PD) detected |
| `perccli_physical_drives_degraded_count` | `ctl` | Physical drives in a non-optimal state |
| `perccli_drive_groups_total` | `ctl` | Total drive groups (DG) |
| `perccli_virtual_drives_total` | `ctl` | Total virtual drives (VD) |
| `perccli_virtual_drives_degraded_count` | `ctl` | Virtual drives in a non-optimal state |
| `perccli_dimmer_switch_status` | `ctl` | Power-saving Dimmer Switch status (1=On, 0=other) |
| `perccli_advanced_software_options_count` | `ctl` | Active Advanced Software Options (ASO) count |

### 💽 Virtual drive metrics
| Metric | Labels | Description |
| --- | --- | --- |
| `perccli_virtual_drive_info` | `ctl`, `dg`, `vd`, `type`, `access`, `cache`, `cac`, `name`, `os_device`, `naa_id` | Virtual drive metadata (value is always 1) |
| `perccli_virtual_drive_state` | `ctl`, `dg`, `vd` | Virtual drive state (1=Optl, 2=Degraded, 3=Offline, 0=other) |
| `perccli_virtual_drive_consistent_status` | `ctl`, `dg`, `vd` | Consistency status (1=Yes/Consistent, 0=No/other) |
| `perccli_virtual_drive_scc_status` | `ctl`, `dg`, `vd` | Scheduled Consistency Check status (1=ON, 0=OFF) |
| `perccli_virtual_drive_size_bytes` | `ctl`, `dg`, `vd` | Virtual drive size in bytes |

### 🧱 Physical drive metrics
| Metric | Labels | Description |
| --- | --- | --- |
| `perccli_physical_drive_info` | `ctl`, `vd`, `eid`, `slt`, `did`, `dg`, `intf`, `med`, `model`, `type`, `sed`, `pi` | Physical drive metadata (value is always 1) |
| `perccli_physical_drive_state` | `ctl`, `vd`, `eid`, `slt` | Physical drive state (1=Onln, 2=Offln, 3=Rbld, 4=GHS, 5=UGood, 0=other) |
| `perccli_physical_drive_size_bytes` | `ctl`, `vd`, `eid`, `slt` | Physical drive size in bytes |
| `perccli_physical_drive_sector_size_bytes` | `ctl`, `vd`, `eid`, `slt` | Physical drive sector size in bytes |
| `perccli_physical_drive_sp_status` | `ctl`, `vd`, `eid`, `slt` | Spare status (0=not spare, 1=HS dedicated, 2=PS global) |

## ⚠️ Known Limitations
- `StartMode=Process` is not implemented yet, so local `perccli64` execution is not available.
- SSH mode requires the remote user to run `sudo perccli64 ...` successfully (typically needs sudoers configuration).
- When running on Linux, configure a passwordless sudoers rule for `perccli64` (for example, `/etc/sudoers.d/perccli64` with `your_user ALL=(root) NOPASSWD: /usr/sbin/perccli64`).

## 🔐 Security Notes
- Avoid storing SSH passwords in plain text (prefer environment variables, secret managers, or external config injection in your deployment).
- If you must use `sudo`, restrict it to `perccli64` and avoid broad `NOPASSWD: ALL`.

## 📄 License
MIT License, see [LICENSE](LICENSE).
