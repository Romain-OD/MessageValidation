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
docker run -d -p 1883:1883 eclipse-mosquitto:2 mosquitto -c /mosquitto-no-auth.conf
dotnet run --project examples/MessageValidation.SparkplugB.Example
# other broker: dotnet run --project ... -- --Broker my-broker.local
```

## Expected output

```
✔ NBIRTH seq=0 from Line1-PLC: bdSeq=0, Node Control/Rebirth=False, Temperature=21.0, Running=True
✔ NDATA seq=1 from Line1-PLC: #1=21.5
✔ NDATA seq=2 from Line1-PLC: #1=22.5
✔ NDATA seq=3 from Line1-PLC: #1=23.5
Validation failed for spBv1.0/Plant1/NDATA/Line1-PLC: metrics[0]: Value StringValue does not match declared datatype Double.
Validation failed for spBv1.0/Plant1/NDATA/Line1-PLC: seq: Out-of-order seq for Plant1/Line1-PLC: expected 5 but was 6.
Validation failed for spBv1.0/Plant1/NDATA/Line1-PLC/Pump1: topic: NDATA topics must not have a device_id segment.
✔ NDEATH seq=0 from Line1-PLC: bdSeq=0
```
