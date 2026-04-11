using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Drive.Single;
using TopSpeed.Drive.TimeTrial;
using TopSpeed.Drive;
using TopSpeed.Tracks;
using TopSpeed.Localization;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Parsing;
using CoreRaceMode = TopSpeed.Core.DriveMode;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void PrepareQuickStart()
        {
            _setup.Mode = CoreRaceMode.QuickStart;
            _setup.ClearSelection();
            _selection.SelectRandomTrackAny(_settings.RandomCustomTracks);
            _selection.SelectRandomVehicle();
            var vehicleIndex = _setup.VehicleIndex ?? 0;
            if (TryResolveTransmissionChoice(vehicleIndex, _setup.VehicleFile, automaticRequested: true, out var automaticTransmission))
            {
                _setup.Transmission = automaticTransmission ? TransmissionMode.Automatic : TransmissionMode.Manual;
            }
            else if (TryResolveTransmissionChoice(vehicleIndex, _setup.VehicleFile, automaticRequested: false, out automaticTransmission))
            {
                _setup.Transmission = automaticTransmission ? TransmissionMode.Automatic : TransmissionMode.Manual;
            }
            else
            {
                _setup.Transmission = TransmissionMode.Automatic;
            }
        }

        private void QueueDriveStart(CoreRaceMode mode)
        {
            _pendingDriveStart = true;
            _pendingMode = mode;
        }

        private void StartDrive(CoreRaceMode mode)
        {
            FadeOutMenuMusic();
            var track = string.IsNullOrWhiteSpace(_setup.TrackNameOrFile)
                ? TrackList.RaceTracks[0].Key
                : _setup.TrackNameOrFile!;
            var trackId = TrackId.FromSelection(track);
            var vehicleIndex = _setup.VehicleIndex ?? 0;
            var vehicleFile = _setup.VehicleFile;
            var automaticRequested = _setup.Transmission == TransmissionMode.Automatic;
            if (!TryResolveTransmissionChoice(vehicleIndex, vehicleFile, automaticRequested, out var automatic))
            {
                _state = AppState.Menu;
                _menu.FadeInMenuMusic(force: true);
                ShowMessageDialog(
                    LocalizationService.Mark("Transmission not supported"),
                    LocalizationService.Mark("The selected vehicle does not support the selected transmission mode."),
                    Array.Empty<string>());
                return;
            }

            try
            {
                switch (mode)
                {
                    case CoreRaceMode.TimeTrial:
                        _timeTrial?.FinalizeSession();
                        _timeTrial?.Dispose();
                        _timeTrial = null;

                        var timeTrial = _driveSessionFactory.CreateTimeTrial(
                            track,
                            trackId,
                            automatic,
                            _settings.NrOfLaps,
                            vehicleIndex,
                            vehicleFile,
                            _input.VibrationDevice);
                        timeTrial.Initialize();
                        _timeTrial = timeTrial;
                        _state = AppState.TimeTrial;
                        _speech.Speak(LocalizationService.Mark("Time trial."));
                        break;
                    case CoreRaceMode.QuickStart:
                    case CoreRaceMode.SingleRace:
                        _singleRace?.FinalizeSession();
                        _singleRace?.Dispose();
                        _singleRace = null;

                        var singleRace = _driveSessionFactory.CreateSingleRace(
                            track,
                            automatic,
                            _settings.NrOfLaps,
                            vehicleIndex,
                            vehicleFile,
                            _input.VibrationDevice);
                        singleRace.Initialize(Algorithm.RandomInt(_settings.NrOfComputers + 1));
                        _singleRace = singleRace;
                        _state = AppState.SingleRace;
                        _speech.Speak(mode == CoreRaceMode.QuickStart
                            ? LocalizationService.Mark("Quick start.")
                            : LocalizationService.Mark("Single race."));
                        break;
                }
            }
            catch (TrackLoadException ex)
            {
                HandleTrackLoadFailure(ex);
            }
        }

        private static bool TryResolveTransmissionChoice(
            int vehicleIndex,
            string? vehicleFile,
            bool automaticRequested,
            out bool automaticTransmission)
        {
            automaticTransmission = automaticRequested;
            TransmissionType primaryType;
            TransmissionType[] supportedTypes;

            if (string.IsNullOrWhiteSpace(vehicleFile))
            {
                var index = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
                var vehicle = VehicleCatalog.Vehicles[index];
                primaryType = vehicle.PrimaryTransmissionType;
                supportedTypes = vehicle.SupportedTransmissionTypes ?? new[] { primaryType };
            }
            else
            {
                if (!VehicleTsvParser.TryLoadFromFile(vehicleFile!, out var parsed, out _))
                    return false;

                primaryType = parsed.PrimaryTransmissionType;
                supportedTypes = parsed.SupportedTransmissionTypes;
            }

            if (!TransmissionSelect.TryResolveRequested(automaticRequested, primaryType, supportedTypes, out var resolvedType))
                return false;

            automaticTransmission = resolvedType != TransmissionType.Manual;
            return true;
        }

        private void HandleTrackLoadFailure(TrackLoadException ex)
        {
            _state = AppState.Menu;
            _menu.FadeInMenuMusic(force: true);

            var items = new List<string>();
            if (ex.Details != null && ex.Details.Count > 0)
            {
                for (var i = 0; i < ex.Details.Count; i++)
                    items.Add(ex.Details[i]);
            }
            else
            {
                items.Add(ex.Message);
            }

            ShowMessageDialog(
                LocalizationService.Mark("Track load error"),
                LocalizationService.Mark("The selected track could not be loaded. The drive session was not started."),
                items);
        }
    }
}






