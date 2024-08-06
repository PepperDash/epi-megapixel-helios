![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# UNDER DEVELOPMENT
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
    "name": "Megapixel Helios",
    "type": "megapixelhelios",
    "properties": {
        "control": {
            "method": "{http || https}",
            "tcpSshProperties": {
                "address": "192.168.101",
                "port": 80,
                "username": "{as defined in HELIOS web application}",
                "password": "{as defined in HELIOS web application}"
            }
        }
    }
}
```

If `port` is only needed when overriding the default HTTP `80` or HTTPS `443`.

### Bridge

```json
{
	"key": "devices-bridge",
	"uid": 26,
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
| Join Number | Join Span | Description          | Type    | Capabilities |
| ----------- | --------- | -------------------- | ------- | ------------ |
| 1           | 1         | Power Off (blackout) | Digital | ToFromSIMPL  |
| 2           | 1         | Power On (blackout)  | Digital | ToFromSIMPL  |
| 50          | 1         | Is Online            | Digital | ToSIMPL      |

### Analogs
| Join Number | Join Span | Description                 | Type         | Capabilities |
| ----------- | --------- | --------------------------- | ------------ | ------------ |
| 21          | 1         | Preset select by `presetId` | AnalogSerial | ToFromSIMPL  |

### Serials
| Join Number | Join Span | Description                   | Type   | Capabilities |
| ----------- | --------- | ----------------------------- | ------ | ------------ |
| 1           | 1         | Device Name                   | Serial | ToSIMPL      |
| 21          | 1         | Preset select by `presetName` | Serial | ToSimpl      |

## DEVJSON Commands

Public Methods that can be used with `devjson` to test controls.  

```json
devjson:1 {"deviceKey":"display-1","methodName":"PowerOn","params":[]}
devjson:1 {"deviceKey":"display-1","methodName":"PowerOff","params":[]}
devjson:1 {"deviceKey":"display-1","methodName":"GetPresetsList","params":[]}
devjson:1 {"deviceKey":"display-1","methodName":"RecallPresetById","params":[1]} // example: preesetId '1'
devjson:1 {"deviceKey":"display-1","methodName":"RecallPresetByName","params":["full"]} // example: preesetName 'full'
```

