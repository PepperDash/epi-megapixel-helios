![PepperDash Essentials Plugin Logo](/images/essentials-plugin-blue.png)

![PepperDash Essentials](https://img.shields.io/badge/PepperDash%20Essentials-≥%20v3.0.0-blue)
![.NET](https://img.shields.io/badge/.NET%20-8.0-512BD4)
![Crestron](https://img.shields.io/badge/Crestron-4--Series-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

# Megapixel Helios (c) 2024

Minimum Helios firmware version = `24.11.0.23030`

## License

Provided under MIT license

## Overview

PepperDash Essentials plugin for the Megapixel Helios LED display controller. Provides power control, brightness, input selection, preset recall, test patterns, HDMI hotplug, and redundancy management via the Helios REST API.

- Supports HTTP and HTTPS connections with automatic self-signed certificate trust
- Basic and Digest authentication
- Configurable polling interval
- SIMPL bridge integration via `eiscApiAdvanced`

## 3.0.0 Release Notes

> **Not yet tested on hardware.** This release has been built and reviewed but has not been
> validated against a Helios processor.

### Requirements

PepperDash Essentials **3.0.0 or later** (.NET 8) on a Crestron 4-Series processor. For Essentials 2.x,
use plugin 2.x.

### Upgrading

Remove any older plugin file from the processor's plugins folder before loading 3.0.0:

- `PepperDash.Essentials.Plugins.Megapixel.Helios*` (2.x releases)
- `PepperDash.Essentials.Plugin.Megapixel.Helios*` (1.x releases)
- `PepperDash.Plugins.Megapixel*` (`2.0.0-4series-migration` prereleases)

Otherwise more than one copy loads and registers the `megapixelHelios` type.

### What's new

- HDMI/SDI input resolution and format on serial joins 31–34 and 41–44.
- `IsOnline` (digital join 50) tracks the device again: online on a successful response, offline on
  an error response or a failed request.
- `ResponseCode` (analog join 3) is updated again.
- A missing or invalid `control.tcpSshProperties.address` is reported by the factory with a specific
  error; a missing `brightness` block falls back to the default levels (75/50/20).
- Repeated request failures are logged up to five times, then suppressed until the device responds.

### Unreleased (feature/v3-migration)

- **Mobile Control messenger** at `/device/{key}`:
  - State: `isOnline`, `powerIsOn`, `currentInput`, `inputs`, `testPatternIsOn`, `brightness`, `brightnessLevel`, `brightnessLevels`, `redundancy { role, state, desired, mode }`.
  - Actions: `/fullStatus`, `/powerOn`, `/powerOff`, `/selectInput {input}`, `/testPattern {on}`, `/brightness {level}` (`high`, `medium`, `low` or a percent), `/setActiveController {controller}` (`main` or `backup`).
  - The core power, display and communication monitor messengers are registered too.
- **Inputs are routing input ports.**
  - Configure them with `inputs` (default HDMI 1, HDMI 2, SDI 1, SDI 2 as `hdmiIn1`, `hdmiIn2`, `sdiIn1`, `sdiIn2`).
  - A tie line from a decoder lands on the port, and routing to the processor selects that input.
- **`ICommunicationMonitor`**: OK while the processor answers; warning after `warningTimeoutMs` (3 minutes) and error after `errorTimeoutMs` (5 minutes) without a response.
- **`commandDelayAfterPowerOnMs`**: commands sent within this window after a power on are delayed, not dropped, and are sent one at a time in the order they were issued once the hold ends. Polls and power commands are not held.
- **Brightness levels are per processor.** They used to be static, so the last processor configured set the levels for every processor in the system.
- **Bug fix:** a test pattern PATCH no longer also sends `"input": null` and `"type": null`.
- **Mock:** the new `mockMegapixelHelios` type has the same ports, messenger and interfaces and keeps its state in memory, for bench testing. Add `"mockRedundancyRole": "backup"` for the backup processor of a pair.

### Known limitations

- `ResponseContent` (serial join 3) is not supported (see the Join Map note).
- The active controller override uses the redundancy *state* commands (`state: main|backup`), as the bridge's SetRedundancyStateToMain/ToBackup joins do. This has not yet been verified against a redundant pair.

## Configuration Object

### Device

MinimumEssentialsFrameworkVersion = `3.0.0`

Type: `megapixelHelios`

```json
{
	"key": "display-1",
	"uid": 1,
	"name": "Megapixel Helios",
	"group": "display",
	"type": "megapixelHelios",
	"properties": {
		"control": {
		"method": "https",
		"tcpSshProperties": {
			"address": "192.168.1.100",
			"port": 443,
			"username": "{as defined in HELIOS web application}",
			"password": "{as defined in HELIOS web application}",
			"autoReconnect": true,
			"autoReconnectIntervalMs": 10000
			}
		},
		"pollTimeMs": 15000,
		"brightness":{
		"high": 60,
		"medium": 40,
		"low": 20
		},
		"presets":[
		{ "label": "HDMI1", "presetName": "HDMI1" },
		{ "label": "HDMI2", "presetName": "HDMI2" },
		{ "label": "SDI1" , "presetName": "SDI1"  },
		{ "label": "SDI2" , "presetName": "SDI2"  }
		],
		"inputs": [
		{ "key": "hdmiIn1", "name": "HDMI 1", "ingestName": "hdmi1" },
		{ "key": "hdmiIn2", "name": "HDMI 2", "ingestName": "hdmi2" },
		{ "key": "sdiIn1",  "name": "SDI 1",  "ingestName": "sdi1"  },
		{ "key": "sdiIn2",  "name": "SDI 2",  "ingestName": "sdi2"  }
		],
		"commandDelayAfterPowerOnMs": 5000,
		"warningTimeoutMs": 180000,
		"errorTimeoutMs": 300000
	}
}
```

Mock, for bench testing (no `control` block needed):

```json
{ "key": "display-1-backup", "name": "Helios Backup", "group": "display", "type": "mockMegapixelHelios",
  "properties": { "mockRedundancyRole": "backup", "brightness": { "high": 60, "medium": 40, "low": 20 } } }
```

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| control.method | string | - | `http` or `https` |
| control.tcpSshProperties.address | string | - | Device IP address |
| control.tcpSshProperties.port | int | 443/80 | Only needed when overriding the default HTTP `80` or HTTPS `443` |
| control.tcpSshProperties.username | string | - | Authentication username |
| control.tcpSshProperties.password | string | - | Authentication password |
| pollTimeMs | long | 15000 | Polling interval in milliseconds |
| brightness.high | ushort | - | Brightness level for "high" preset (0-100) |
| brightness.medium | ushort | - | Brightness level for "medium" preset (0-100) |
| brightness.low | ushort | - | Brightness level for "low" preset (0-100) |
| presets | array | - | List of preset objects with `label`, `presetName`, and/or `presetId` |
| inputs | array | HDMI 1, HDMI 2, SDI 1, SDI 2 | Each `{ key, name, ingestName, connectionType? }`: a routing input port `key`, the `name` a panel shows, the `dev.ingest.input` value |
| commandDelayAfterPowerOnMs | int | 0 | Hold other commands this long after a power on |
| warningTimeoutMs / errorTimeoutMs | long | 180000 / 300000 | Communication monitor: no response for this long is a warning / error |
| mockRedundancyRole | string | `main` | `mockMegapixelHelios` only: `main` or `backup` |

### Bridge

```json
{
	"key": "devices-bridge",
	"uid": 2,
	"group": "api",
	"type": "eiscApiAdvanced",
	"properties": {
		"control": {
			"tcpSshProperties": {
				"address": "127.0.0.2",
				"port": 0
			},
			"ipid": "a6",
			"method": "ipidTcp"
		},
		"devices": [
			{ "deviceKey": "display-1"     , "joinStart": 1 }			
		]
	}
}
```

## Join Map

### Digitals
| Join Number | Join Span | Description              | Type    | Capabilities |
| ----------- | --------- | --------------------     | ------- | ------------ |
| 1           | 1         | Power Off (blackout)     | Digital | ToFromSIMPL  |
| 2           | 1         | Power On (blackout)      | Digital | ToFromSIMPL  |
| 3           | 1         | Poll (public API)        | Digital | FromSIMPL    |
| 4           | 1         | Redundancy Role Main     | Digital | ToFromSIMPL  |
| 5           | 1         | Redundancy Role Backup   | Digital | ToFromSIMPL  |
| 6           | 1         | Redundancy Role Offline  | Digital | ToFromSIMPL  |
| 7           | 1         | Redundancy State Main    | Digital | FromSIMPL    |
| 8           | 1         | Redundancy State Backup  | Digital | FromSIMPL    |
| 7           | 1         | Redundancy State Active  | Digital | ToSIMPL      |
| 8           | 1         | Redundancy State Standby | Digital | ToSIMPL      |
| 9           | 1         | Redundancy State Mixed   | Digital | ToSIMPL      |
| 31          | 1         | Test Pattern On          | Digital | ToFromSIMPL  |
| 32          | 1         | Test Pattern Off         | Digital | ToFromSIMPL  |
| 33          | 1         | Brightness High          | Digital | ToFromSIMPL  |
| 34          | 1         | Brightness Medium        | Digital | ToFromSIMPL  |
| 35          | 1         | Brightness Low           | Digital | ToFromSIMPL  |
| 41          | 1         | Hdmi1 Hotplug            | Digital | FromSIMPL    |
| 42          | 1         | Hdmi2 Hotplug            | Digital | FromSIMPL    |
| 41          | 1         | Hdmi1 Invalid            | Digital | ToSIMPL      |
| 42          | 1         | Hdmi2 Invalid            | Digital | ToSIMPL      |
| 50          | 1         | Is Online                | Digital | ToSIMPL      |

### Analogs
| Join Number | Join Span | Description                 | Type         | Capabilities |
| ----------- | --------- | --------------------------- | ------------ | ------------ |
| 3           | 1         | Response Code               | AnalogSerial | ToSIMPL      |
| 21          | 1         | Preset select by `presetId` | AnalogSerial | ToFromSIMPL  |
| 33          | 1         | Brightness                  | AnalogSerial | ToFromSIMPL  |

### Serials
| Join Number | Join Span | Description                   | Type   | Capabilities |
| ----------- | --------- | ----------------------------- | ------ | ------------ |
| 1           | 1         | Device Name                   | Serial | ToSIMPL      |
| 3           | 1         | Response Content (**not supported**, see note) | Serial | ToSIMPL      |
| 10          | 1         | Input select by `inputName`   | Serial | ToFromSIMPL  |
| 21          | 1         | Preset select by `presetName` | Serial | ToFromSimpl  |
| 31          | 1         | HDMI 1 resolution             | Serial | ToSIMPL      |
| 32          | 1         | HDMI 2 resolution             | Serial | ToSIMPL      |
| 33          | 1         | SDI 1 resolution              | Serial | ToSIMPL      |
| 34          | 1         | SDI 2 resolution              | Serial | ToSIMPL      |
| 41          | 1         | HDMI 1 format                 | Serial | ToSIMPL      |
| 42          | 1         | HDMI 2 format                 | Serial | ToSIMPL      |
| 43          | 1         | SDI 1 format                  | Serial | ToSIMPL      |
| 44          | 1         | SDI 2 format                  | Serial | ToSIMPL      |

> **Serial join 3 (`ResponseContent`) does not work in SIMPL.** The join is still defined in the join map,
> but the plugin never populates it, so it will not update. It was dropped in the 4-Series rework,
> which stopped sending the full JSON response to SIMPL on every poll.

## POINT OF CLARIFICATION

1. The API document (see `docs` folder) tracks both `role` and `state` objects.
2. The `role` API call refers to the long-term role assigned to the device (main vs backup), not typically changed.
3. The `state` API call refers to both the requested and reported state of the device.
4. The device is capable of detecting loss of video from the primary or `main` controller and automatically switches `state` as needed.
5. The device will report current state as either `active`, `standby`, or `mixed`.
6. The various `state` definitions reported above cannot be requested.
7. The only valid `state` the device accepts is `main` or `backup`.
8. The `state` request of `main` vs `backup` should be sent to override the automatic switch reported.
9. Hotplug requests utilize manufacturer private API which may change with firmware.

## API Endpoints

| Method | HTTP | Endpoint | Description |
| ------ | ---- | -------- | ----------- |
| Poll | GET | /api/v1/public | Poll device public API |
| PollPrivateApi | GET | /api/v1/data | Poll device private API |
| PowerOn | PATCH | /api/v1/public | Set blackout to false |
| PowerOff | PATCH | /api/v1/public | Set blackout to true |
| SetBrightness | PATCH | /api/v1/public | Set brightness level (0-100) |
| TestPatternOn/Off | PATCH | /api/v1/public | Enable/disable test pattern |
| RecallPresetById | POST | /api/v1/presets/{id}/apply | Recall preset by numeric ID |
| RecallPresetByName | POST | /api/v1/presets/apply | Recall preset by name |
| RecallInputByName | PATCH | /api/v1/public | Select input by name |
| GetPresetsList | GET | /api/v1/presets/list | Query available presets |
| GetRedundancyState | GET | /api/v1/public?dev.display.redundancy | Query redundancy status |
| SetRedundancyRole* | PATCH | /api/v1/public | Set redundancy role |
| SetRedundancyState* | PATCH | /api/v1/public | Set redundancy state |
| HotplugHdmi1/2 | PATCH | /api/v1/data | Force HDMI hotplug (private API) |

## DEVJSON Commands

Public Methods that can be used with `devjson` to test controls.  

```json
devjson:1 {"deviceKey":"display-1","methodName":"PowerOn"                    ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"PowerOff"                   ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"Poll"                       ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"PollPrivateApi"              ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"GetRedundancyState"         ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyRoleToMain"    ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyRoleToBackup"  ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyRoleToOffline" ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyStateToMain"   ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyStateToBackup" ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetBrightness"              ,"params":["50"   ]}
devjson:1 {"deviceKey":"display-1","methodName":"GetPresetsList"             ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"RecallPresetById"           ,"params":[1      ]}
devjson:1 {"deviceKey":"display-1","methodName":"RecallPresetByName"         ,"params":["full" ]}
devjson:1 {"deviceKey":"display-1","methodName":"TestPatternOn"              ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"TestPatternOff"             ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"RecallInputByName"          ,"params":["hdmi1"]}
devjson:1 {"deviceKey":"display-1","methodName":"HotplugHdmi1"               ,"params":[       ]}
devjson:1 {"deviceKey":"display-1","methodName":"HotplugHdmi2"               ,"params":[       ]}
```


