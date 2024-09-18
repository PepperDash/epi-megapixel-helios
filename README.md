![4Series-Tested](https://img.shields.io/badge/4_Series-Tested-teal.svg)

![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# Megapixel Helios (c) 2024

## License

Provided under MIT license

## Configuration Object

### Device

MinimumEssentialsFrameworkVersion = `1.16.0`

Type: `megapixelhelios`

```json
{
	"key": "display-1",
	"uid": 1,
	"name": "Megapixel Helios",
	"group": "display",
	"type": "megapixelHelios",
	"properties": {
		"control": {
		"method": "http",
		"tcpSshProperties": {
			"address": "192.168.1.100",
			"port": 80,
			"username": "{as defined in HELIOS web application}",
			"password": "{as defined in HELIOS web application}",
			"autoReconnect": true,
			"autoReconnectIntervalMs": 10000
			}
		},
		"brightness":{
		"high": 60,
		"medium": 40,
		"low": 20
		},
		"presets":[
		{ "name": "HDMI1", "presetName": "HDMI1" },
		{ "name": "HDMI2", "presetName": "HDMI2" },
		{ "name": "SDI1" , "presetName": "SDI1"  },
		{ "name": "SDI2" , "presetName": "SDI2"  }
		]
	}
}
```

The `port` object is only needed when overriding the default HTTP `80` or HTTPS `443`.

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
| 4           | 1         | Redundancy Role Main     | Digital | ToFromSIMPL  |
| 5           | 1         | Redundancy Role Backup   | Digital | ToFromSIMPL  |
| 6           | 1         | Redundancy Role Offline  | Digital | ToFromSIMPL  |
| 7           | 1         | Redundancy State Main    | Digital | FromSIMPL    |
| 7           | 1         | Redundancy State Backup  | Digital | FromSIMPL    |
| 7           | 1         | Redundancy State Active  | Digital | ToSIMPL      |
| 8           | 1         | Redundancy State Standby | Digital | ToSIMPL      |
| 9           | 1         | Redundancy State Mixed   | Digital | ToSIMPL      |
| 31          | 1         | Test Pattern On          | Digital | ToFromSIMPL  |
| 32          | 1         | Test Pattern Off         | Digital | ToFromSIMPL  |
| 33          | 1         | Brightness High          | Digital | ToFromSIMPL  |
| 34          | 1         | Brightness Medium        | Digital | ToFromSIMPL  |
| 35          | 1         | Brightness Low           | Digital | ToFromSIMPL  |
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
| 3           | 1         | Response Content              | Serial | ToSIMPL      |
| 21          | 1         | Preset select by `presetName` | Serial | ToFromSimpl  |

## POINT OF CLARIFICATION ##

1. The API document (see `docs` folder) tracks both `role` and `state` objects.
2. The `role` API call refers to the long-term role of the video processor assigned to the device (main vs backup). This is not typically changed.
3. The `state` API call refers to both the requested and the actual reported state of the video processor.
4. The device is capable of detecting loss of video from the primary or `main` controller and automatically switches `states` as needed.
5. The device will report it's current state as either `active`, `standby`, or `mixed`. The `states` reported cannot be requested.
6. The only valid `state` the device accepts is `main` or `backup`.

## DEVJSON Commands

Public Methods that can be used with `devjson` to test controls.  

```json
devjson:1 {"deviceKey":"display-1","methodName":"PowerOn"                    ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"PowerOff"                   ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"GetRedundancyState"         ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyRoleToMain"    ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyRoleToBackup"  ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyRoleToOffline" ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyStateToMain"   ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetRedundancyStateToBackup" ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"SetBrightness"              ,"params":["50"  ]} // example: brightness '50'
devjson:1 {"deviceKey":"display-1","methodName":"GetPresetsList"             ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"RecallPresetById"           ,"params":[1     ]} // example: preesetId '1'
devjson:1 {"deviceKey":"display-1","methodName":"RecallPresetByName"         ,"params":["full"]} // example: preesetName 'full'
devjson:1 {"deviceKey":"display-1","methodName":"TestPatternOn"              ,"params":[      ]}
devjson:1 {"deviceKey":"display-1","methodName":"TestPatternOff"             ,"params":[      ]}
```

