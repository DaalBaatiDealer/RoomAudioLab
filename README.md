# RoomAudioLab

RoomAudioLab is a native Windows prototype for creating a simple front-and-rear spatial sound experience with two separate speakers. Connect two speakers, choose which one is physically in front and which is behind you, then place a sound source in the room view and hear it shift between those positions.

It is designed for experimenting with speaker placement around you. It does not create headphone-style 3D audio or automatically understand where sounds are in a recording. For now, you manually position one source along the front-to-rear axis, and the app blends its level between the two speakers. Small timing differences between devices can be compensated with per-speaker delay controls.

## What it does

- Plays a built-in 440 Hz test tone or a local audio file through two selected Windows audio outputs.
- Lets you assign the outputs to the speakers' physical front and rear positions.
- Lets you drag a source marker between front and rear; the app shifts the relative level across both speakers to create the spatial movement.
- Includes a separate level and sync delay control for each speaker (0–500 ms).
- Uses WASAPI shared-mode output through NAudio. The Windows default output can be selected like any other device.

## Try it

1. Connect both speakers and make sure Windows lists them as playback devices.
2. Download `RoomAudioLab-win-x64.zip` from **Releases** and extract it.
3. Run `RoomAudioLab.exe`.
4. Assign the speaker physically in front under **Front Speaker**, and the one behind you under **Rear Speaker**.
5. Click **Play test tone** and drag the orange source dot toward FRONT and REAR to hear the sound move between the speakers.
6. Adjust **Sync delay** by ear if the speakers sound slightly out of sync. Add delay to the speaker that sounds earlier; Bluetooth latency varies by device and connection.
7. Click **Open audio file…** to try a local file with the same manually controlled front/rear positioning. Click **Stop playback** before changing endpoints.

## Current limitations

The app remembers no speaker IDs yet, so check the two selections after restarting it. It plays audio opened inside the app; it does not capture or redirect sound from other applications. Source positioning is manual and limited to front and rear. It does not analyze ordinary stereo recordings to find where a voice or instrument is located, and it does not yet support spatial/object audio formats or multichannel channel routing. The result is a simple two-speaker spatial effect, not headphone-style 3D audio.

## Build from source

Requires the .NET 8 SDK on Windows:

```powershell
dotnet publish RoomAudioLab.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The published executable is self-contained and currently unsigned. Windows may show its normal warning for software from an unknown publisher.

## Project status

Early prototype. Contributions and issue reports are welcome. Third-party source attribution is in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
