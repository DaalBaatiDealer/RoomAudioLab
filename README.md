**Room Audio Lab** is a native Windows prototype for sending sound to two playback devices and controlling how loud it is at a front and rear position.

## What it does

- Sends its own playback directly to two selected Windows audio outputs.
- Includes a 440 Hz test tone and local audio-file playback.
- Lets you place the sound source between front and rear by dragging the dot; the two output levels shift smoothly as it moves.
- Provides independent output level and sync delay controls (0–500 ms).
- Uses WASAPI shared-mode output through NAudio. The Windows default output can be selected like any other device.

## Try it

1. Connect both speakers and make sure Windows lists them as playback devices.
2. Download the latest `RoomAudioLab-win-x64.zip` from **Releases** and extract it.
3. Run `RoomAudioLab.exe`.
4. Select the speaker physically in front under **Front Speaker**, and the rear speaker under **Rear Speaker**.
5. Click **Play test tone**. Drag the orange dot toward FRONT and REAR and listen for the level shift.
6. Adjust each output's **Sync delay** by ear to compensate for device latency. Add delay to the speaker that sounds earlier; Bluetooth latency varies by device and connection.
7. Click **Open audio file…** to play a local file through the same room controls. Click **Stop playback** before changing endpoints.

The app remembers no speaker IDs yet, so check the two selections after restarting it. It does not capture or redirect audio from other applications. It cannot infer object positions from ordinary stereo audio; the source marker is manual. Spatial/object audio formats and multichannel channel routing are not implemented yet.

## Build from source

Requires the .NET 8 SDK on Windows:

```powershell
dotnet publish RoomAudioLab.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The published executable is self-contained and currently unsigned. Windows may show its normal warning for software from an unknown publisher.

## Project status

Early prototype. Contributions and issue reports are welcome. Third-party source attribution is in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
