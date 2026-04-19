# 🧰 perccli_exporter

[English](README.md) | [简体中文](README.zh-CN.md)

## 📌 Overview
- A lightweight Prometheus exporter that exposes Dell PERC / MegaRAID status by parsing `perccli64` JSON output.
- Provides `/metrics` for Prometheus scraping (text format `version=0.0.4`).

## ✨ Features
- Controller-level metrics (count, model metadata, health, BBU, patrol read, ports, PD/VD stats).
- Virtual drive (VD) metrics (metadata, state, consistency, scheduled CC, size).
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
- `perc_controller_count`
- `perc_controller_info{ctl,model}`
- `perccli_controller_health_status{ctl}`
- `perccli_bbu_status{ctl}`
- `perccli_patrol_read_status{ctl}`
- `perccli_emergency_hot_spare_status{ctl}`
- `perccli_ports_total{ctl}`
- `perccli_physical_drives_total{ctl}`
- `perccli_physical_drives_degraded_count{ctl}`
- `perccli_drive_groups_total{ctl}`
- `perccli_virtual_drives_total{ctl}`
- `perccli_virtual_drives_degraded_count{ctl}`
- `perccli_dimmer_switch_status{ctl}`
- `perccli_advanced_software_options_count{ctl}`

### 💽 Virtual drive metrics
- `perccli_virtual_drive_info{ctl,dg,vd,type,access,cache,cac,name}`
- `perccli_virtual_drive_state{ctl,dg,vd}`
- `perccli_virtual_drive_consistent_status{ctl,dg,vd}`
- `perccli_virtual_drive_scc_status{ctl,dg,vd}`
- `perccli_virtual_drive_size_bytes{ctl,dg,vd}`

## ⚠️ Known Limitations
- `StartMode=Process` is not implemented yet, so local `perccli64` execution is not available.
- SSH mode requires the remote user to run `sudo perccli64 ...` successfully (typically needs sudoers configuration).

## 🔐 Security Notes
- Avoid storing SSH passwords in plain text (prefer environment variables, secret managers, or external config injection in your deployment).
- If you must use `sudo`, restrict it to `perccli64` and avoid broad `NOPASSWD: ALL`.

## 📄 License
MIT License, see [LICENSE](LICENSE).
