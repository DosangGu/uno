﻿using System;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using CoreMidi;
using MidiInfo = CoreMidi.Midi;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort : IDisposable
	{
		private MidiEndpoint _endpoint;
		private MidiClient _client;
		private MidiPort _port;

		private MidiOutPort(MidiEndpoint endpoint)
		{
			_endpoint = endpoint;
			_client = new MidiClient(Guid.NewGuid().ToString());
		}

		public string DeviceId { get; private set; }

		public static IAsyncOperation<IMidiOutPort> FromIdAsync(string deviceId) =>
			FromIdInternalAsync(deviceId).AsAsyncOperation();

		internal async Task OpenAsync()
		{
			var completionSource = new TaskCompletionSource<MidiDevice>();
			_port = _client.CreateOutputPort(_endpoint.EndpointName);
		}

		public void SendMessage(IMidiMessage midiMessage)
		{
			if (midiMessage is null)
			{
				throw new ArgumentNullException(nameof(midiMessage));
			}

			SendBufferInternal(midiMessage.RawData, midiMessage.Timestamp);
		}

		public void SendBuffer(IBuffer midiData)
		{
			if (midiData is null)
			{
				throw new ArgumentNullException(nameof(midiData));
			}

			SendBufferInternal(midiData, TimeSpan.Zero);
		}

		public void Dispose()
		{
			_port?.Disconnect(_endpoint);
			_port?.Dispose();
			_client?.Dispose();
			_endpoint?.Dispose();
			_port = null;
			_client = null;
			_endpoint = null;
		}

		private static async Task<IMidiOutPort> FromIdInternalAsync(string deviceId)
		{
			var deviceIdentifier = ValidateAndParseDeviceId(deviceId);

			var provider = new MidiOutDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeEndpoint(deviceIdentifier.Id);
			if (nativeDeviceInfo == null)
			{
				throw new InvalidOperationException(
					"Given MIDI out device does not exist or is no longer connected");
			}

			var port = new MidiOutPort(nativeDeviceInfo);
			await port.OpenAsync();
			return port;
		}

		private void SendBufferInternal(IBuffer midiData, TimeSpan timestamp)
		{
			if (_port == null)
			{
				throw new InvalidOperationException("Output port is not initialized.");
			}

			var data = midiData.ToArray();

			var packet = new MidiPacket(0, data, 0, data.Length);
			var packets = new MidiPacket[] { packet };

			_port.Send(_endpoint, packets);
		}
	}
}
