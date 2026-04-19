# 🧰 perccli_exporter

[简体中文](README.zh-CN.md) | [English](README.md)

## 📌 项目简介
- 一个轻量级 Prometheus Exporter，通过解析 `perccli64` 的 JSON 输出，暴露 Dell PERC / MegaRAID 的状态指标。
- 提供 `/metrics` 供 Prometheus 抓取（文本格式 `version=0.0.4`）。

## ✨ 功能特性
- 控制器（Controller）级别指标：数量、型号元信息、健康状态、BBU、电巡检、端口数、PD/VD 统计等。
- 虚拟盘（VD/RAID 卷）指标：元信息、状态、一致性、计划一致性检查、容量等。
- 物理盘（PD）指标：元信息、状态、容量、扇区大小、热备状态等。
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
| 指标名称 | 标签 | 说明 |
| --- | --- | --- |
| `perc_controller_count` | - | RAID 控制器数量 |
| `perc_controller_info` | `ctl`, `model` | 控制器元信息（值恒为 1） |
| `perccli_controller_health_status` | `ctl` | 控制器整体健康状态（1=Optimal，0=其他） |
| `perccli_bbu_status` | `ctl` | BBU 状态（1=Optimal，0=异常或缺失） |
| `perccli_patrol_read_status` | `ctl` | 计划巡检（Patrol Read）状态（1=On，0=Off） |
| `perccli_emergency_hot_spare_status` | `ctl` | 应急热备（EHS）策略状态（1=启用，0=禁用） |
| `perccli_ports_total` | `ctl` | 控制器物理端口总数 |
| `perccli_physical_drives_total` | `ctl` | 物理盘（PD）总数 |
| `perccli_physical_drives_degraded_count` | `ctl` | 非最佳状态（非 Optimal）的物理盘数量 |
| `perccli_drive_groups_total` | `ctl` | 盘组（DG）总数 |
| `perccli_virtual_drives_total` | `ctl` | 虚拟盘（VD）总数 |
| `perccli_virtual_drives_degraded_count` | `ctl` | 非最佳状态（非 Optimal）的虚拟盘数量 |
| `perccli_dimmer_switch_status` | `ctl` | 省电 Dimmer Switch 状态（1=On，0=其他） |
| `perccli_advanced_software_options_count` | `ctl` | 高级软件选项（ASO）启用数量 |

### 💽 虚拟盘指标
| 指标名称 | 标签 | 说明 |
| --- | --- | --- |
| `perccli_virtual_drive_info` | `ctl`, `dg`, `vd`, `type`, `access`, `cache`, `cac`, `name`, `os_device`, `naa_id` | 虚拟盘元信息（值恒为 1） |
| `perccli_virtual_drive_state` | `ctl`, `dg`, `vd` | 虚拟盘状态（1=Optl，2=Degraded，3=Offline，0=其他） |
| `perccli_virtual_drive_consistent_status` | `ctl`, `dg`, `vd` | 一致性状态（1=Yes/Consistent，0=No/其他） |
| `perccli_virtual_drive_scc_status` | `ctl`, `dg`, `vd` | 计划一致性检查（sCC）状态（1=ON，0=OFF） |
| `perccli_virtual_drive_size_bytes` | `ctl`, `dg`, `vd` | 虚拟盘容量（字节） |

### 🧱 物理盘指标
| 指标名称 | 标签 | 说明 |
| --- | --- | --- |
| `perccli_physical_drive_info` | `ctl`, `vd`, `eid`, `slt`, `did`, `dg`, `intf`, `med`, `model`, `type`, `sed`, `pi` | 物理盘元信息（值恒为 1） |
| `perccli_physical_drive_state` | `ctl`, `vd`, `eid`, `slt` | 物理盘状态（1=Onln，2=Offln，3=Rbld，4=GHS，5=UGood，0=其他） |
| `perccli_physical_drive_size_bytes` | `ctl`, `vd`, `eid`, `slt` | 物理盘容量（字节） |
| `perccli_physical_drive_sector_size_bytes` | `ctl`, `vd`, `eid`, `slt` | 物理盘扇区大小（字节） |
| `perccli_physical_drive_sp_status` | `ctl`, `vd`, `eid`, `slt` | 热备状态（0=非热备，1=专用热备 HS，2=全局热备 PS） |

## ⚠️ 已知限制
- `StartMode=Process` 暂未实现，因此无法在本机直接执行 `perccli64` 来采集数据。
- SSH 模式要求远端用户可以成功执行 `sudo perccli64 ...`（通常需要配置 sudoers）。
- 在 Linux 下运行时，需要为 `perccli64` 配置免密码 sudoers 授权文件（例如创建 `/etc/sudoers.d/perccli64`，内容为 `your_user ALL=(root) NOPASSWD: /usr/sbin/perccli64`）。

## 🔐 安全建议
- 避免在配置文件中明文保存 SSH 密码（建议使用环境变量、密钥管理、或部署时注入配置）。
- 如需使用 `sudo`，建议仅对白名单命令（例如 `perccli64`）开放，而不是放开 `NOPASSWD: ALL`。

## 📄 许可证
MIT License，见 [LICENSE](LICENSE)。
