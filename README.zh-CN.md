# 🧰 perccli_exporter

[简体中文](README.zh-CN.md) | [English](README.md)

## 📌 项目简介
- 一个轻量级 Prometheus Exporter，通过解析 `perccli64` 的 JSON 输出，暴露 Dell PERC / MegaRAID 的状态指标。
- 提供 `/metrics` 供 Prometheus 抓取（文本格式 `version=0.0.4`）。

## ✨ 功能特性
- 控制器（Controller）级别指标：数量、型号元信息、健康状态、BBU、电巡检、端口数、PD/VD 统计等。
- 虚拟盘（VD/RAID 卷）指标：元信息、状态、一致性、计划一致性检查、容量等。
- 后台定时采集，支持配置采集间隔。

## 🧱 工作原理
- 后台服务按间隔轮询 RAID 数据。
- 数据保存在内存快照中，通过 `GET /metrics` 以 Prometheus 指标格式输出。
- 数据来源（StartMode）：
  - `File`：从 `PercCli.Exporter/Data/*.json` 读取示例数据（适合本地开发/演示）。
  - `Ssh`：通过 SSH 在远端执行 `sudo perccli64 ... show J` 并解析 JSON。
  - `Process`：当前未实现（保留占位）。

## ⚙️ 配置说明
配置节点：`PercOption`

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

- `StartMode`：`0=File`，`1=Process`，`2=Ssh`
- `SshConfig`：SSH 模式连接信息
- `PollingInterval`：采集间隔（秒）

## 🚀 快速开始
### 🧪 本地运行（开发）

```bash
dotnet run --project .\PercCli.Exporter\PercCli.Exporter.csproj
```

访问：
- `http://localhost:5000/metrics`（或以控制台输出端口为准）

### 🏗️ 发布（Native AOT）
项目文件已开启 Native AOT + Trim。

```bash
dotnet publish .\PercCli.Exporter\PercCli.Exporter.csproj -c Release -o .\out
```

## 📈 指标列表
### 🎛️ 控制器指标
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

### 💽 虚拟盘指标
- `perccli_virtual_drive_info{ctl,dg,vd,type,access,cache,cac,name}`
- `perccli_virtual_drive_state{ctl,dg,vd}`
- `perccli_virtual_drive_consistent_status{ctl,dg,vd}`
- `perccli_virtual_drive_scc_status{ctl,dg,vd}`
- `perccli_virtual_drive_size_bytes{ctl,dg,vd}`

## ⚠️ 已知限制
- `StartMode=Process` 暂未实现，因此无法在本机直接执行 `perccli64` 来采集数据。
- SSH 模式要求远端用户可以成功执行 `sudo perccli64 ...`（通常需要配置 sudoers）。

## 🔐 安全建议
- 避免在配置文件中明文保存 SSH 密码（建议使用环境变量、密钥管理、或部署时注入配置）。
- 如需使用 `sudo`，建议仅对白名单命令（例如 `perccli64`）开放，而不是放开 `NOPASSWD: ALL`。

## 📄 许可证
MIT License，见 [LICENSE](LICENSE)。
