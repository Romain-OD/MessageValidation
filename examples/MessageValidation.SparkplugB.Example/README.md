# MessageValidation.SparkplugB.Example

This is a worker service with two parts:

- **`SparkplugConsumer`**: an MQTTnet client wired to the MessageValidation pipeline and subscribed to `spBv1.0/#`.
- **`EdgeNodeSimulator`**: a fake PLC (`Plant1/Line1-PLC`) that publishes:
  1. ✔ NBIRTH, then 3 NDATA messages
  2. ✘ NDATA with a string value for a metric declared as `Double`
  3. ✘ NDATA with a skipped `seq`
  4. ✘ NDATA published on a device-style topic
  5. ✔ NDEATH

## Run

```bash
pwsh examples/run-all.ps1 sparkplugb        # podman: mosquitto + this example
# or against your own broker:
dotnet run --project examples/MessageValidation.SparkplugB.Example -- --Broker my-broker.local
```

## Expected output

```
✔ [spBv1.0/Plant1/NBIRTH/Line1-PLC] NBIRTH seq=0 from Line1-PLC: bdSeq=0, Node Control/Rebirth=False, Temperature=21.0, Running=True
✔ [spBv1.0/Plant1/NDATA/Line1-PLC] NDATA seq=1 from Line1-PLC: #1=21.5
✔ [spBv1.0/Plant1/NDATA/Line1-PLC] NDATA seq=2 from Line1-PLC: #1=22.5
✔ [spBv1.0/Plant1/NDATA/Line1-PLC] NDATA seq=3 from Line1-PLC: #1=23.5
✘ [spBv1.0/Plant1/NDATA/Line1-PLC] metrics[0]: Value StringValue does not match declared datatype Double.
✘ [spBv1.0/Plant1/NDATA/Line1-PLC] seq: Out-of-order seq for Plant1/Line1-PLC: expected 5 but was 6.
✘ [spBv1.0/Plant1/NDATA/Line1-PLC/Pump1] topic: NDATA topics must not have a device_id segment.
✔ [spBv1.0/Plant1/NDEATH/Line1-PLC] NDEATH seq=0 from Line1-PLC: bdSeq=0
PASS: 5/5 handled, 3/3 rejected
```