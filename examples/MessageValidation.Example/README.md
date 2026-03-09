# MessageValidation — Example

A runnable console app demonstrating the full **MessageValidation** pipeline.

## What it shows

| Scenario | Source | Expected |
|---|---|---|
| Valid temperature reading | `sensors/kitchen/temperature` | Handler prints the reading |
| Invalid temperature reading | `sensors/bedroom/temperature` | Validation fails, logged as warning |
| Valid device heartbeat | `devices/thermostat-01/status` | Handler prints device status |
| Unknown source | `logs/system/error` | No mapping, logged as warning |
| Deep wildcard match | `devices/floor2/sensor-hub/battery` | `devices/#` matches, handler invoked |

## Features demonstrated

- **Wildcard source mapping** — `sensors/+/temperature` and `devices/#`
- **FluentValidation adapter** — validators auto-discovered via assembly scanning
- **Multiple message types** — `TemperatureReading` and `DeviceHeartbeat`
- **Failure behavior** — `FailureBehavior.Log` for invalid messages
- **DI integration** — standard `IServiceCollection` setup

## Run it

```bash
cd examples/MessageValidation.Example
dotnet run
```

## Expected output

```
══════════════════════════════════════════════════════
  MessageValidation — Example Pipeline
══════════════════════════════════════════════════════

→ Valid temperature reading from sensors/kitchen/temperature:
  ✅ [sensors/kitchen/temperature] Sensor kitchen-01: 22.5°C at 14:30:00

→ Invalid temperature reading (missing SensorId, value=999):
  warn: Validation failed for sensors/bedroom/temperature: SensorId: SensorId is required.; Value: Value must be between -50 and 150.

→ Valid heartbeat from devices/thermostat-01/status:
  ✅ [devices/thermostat-01/status] Device thermostat-01 is online

→ Unknown source (logs/system/error — no mapping registered):
  warn: No mapping found for source logs/system/error

→ Valid heartbeat from devices/floor2/sensor-hub/battery:
  ✅ [devices/floor2/sensor-hub/battery] Device sensor-hub-42 is offline

══════════════════════════════════════════════════════
  Done. Check the output above for pipeline results.
══════════════════════════════════════════════════════
```
