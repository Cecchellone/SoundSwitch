/********************************************************************
* Copyright (C) 2015-2017 Antoine Aflalo
*
* This program is free software; you can redistribute it and/or
* modify it under the terms of the GNU General Public License
* as published by the Free Software Foundation; either version 2
* of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using AudioDefaultSwitcherWrapper;
using NAudio.CoreAudioApi;
using Serilog;
using SoundSwitch.Model;

namespace SoundSwitch.Framework.DeviceCyclerManager.DeviceCycler
{
    public abstract class ADeviceCycler : IDeviceCycler
    {
        private readonly IDictionary<DeviceType, String> _lastDevices = new Dictionary<DeviceType, String>();

        public abstract DeviceCyclerTypeEnum TypeEnum { get; }
        public abstract string Label { get; }

        /// <summary>
        /// Cycle the audio device for the given type
        /// </summary>
        /// <param name="type"></param>
        public abstract bool CycleAudioDevice(DeviceType type);

        /// <summary>
        /// Get the next device that need to be set as Default
        /// </summary>
        /// <param name="audioDevices"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected MMDevice GetNextDevice(ICollection<MMDevice> audioDevices, DeviceType type)
        {
            _lastDevices.TryGetValue(type, out var lastDeviceId);
            MMDevice lastDevice = null;
            if (lastDeviceId != null)
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    try
                    {
                        lastDevice = enumerator.GetDevice(lastDeviceId);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            var defaultDev = lastDevice ??
                             audioDevices.FirstOrDefault(device => AudioController.IsDefault(device.ID, type, DeviceRole.Console)) ??
                             audioDevices.Last();
            var next = audioDevices.SkipWhile((device, i) => device.ID != defaultDev.ID).Skip(1).FirstOrDefault() ??
                       audioDevices.ElementAt(0);
            return next;
        }

        /// <summary>
        /// Attempts to set active device to the specified name
        /// </summary>
        /// <param name="device"></param>
        public bool SetActiveDevice(MMDevice device)
        {

            Log.Information("Set Default device: {Device}", device);
            if (!AppModel.Instance.SetCommunications)
            {
                AudioController.SwitchTo(device.ID, DeviceRole.Console);
                AudioController.SwitchTo(device.ID, DeviceRole.Multimedia);
            }
            else
            {
                Log.Information("Set Default Communication device: {Device}", device);
                AudioController.SwitchTo(device.ID, DeviceRole.All);
            }
            _lastDevices[(DeviceType)device.DataFlow] = device.ID;
            return true;

        }
    }
}