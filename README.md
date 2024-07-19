![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# UNDER DEVELOPMENT
# Megapixel Helios (c) 2024

## License

Provided under MIT license

## Configuration Object

### Device

```json
{
    "key": "videoWall-1",
    "name": "Video Wall",
    "type": "",
    "properties": {
        "control": {
            "method": "{http, port 80 || https, port 443}",
            "tcpSshProperties": {
                "address": "192.168.101",
                "port": 80,
                "username": "{as defined in HELIOS web application}",
                "password": "{as defined in HELIOS web application}"
            }
        },
        "pollTimeMs": 60000
    }
}
```

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
			{ "deviceKey": "videoWall-1"     , "joinStart": 1 }			
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


