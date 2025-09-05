﻿using System;
using System.Collections.Generic;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Audio.Models;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Models.Player;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Settings;
using NAudio.Wave;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Audio.Providers;

public class ClientAudioProvider : AudioProvider
{
    private readonly Random _random = new();
    public static readonly int MaxSamples = Constants.OUTPUT_SAMPLE_RATE * 120 / 1000; // 120ms is the max opus frame size.

    //progress per radio
    private readonly Dictionary<string, int>[] ambientEffectProgress;

    private readonly CachedAudioEffectProvider audioEffectProvider = CachedAudioEffectProvider.Instance;
    private readonly ClientTransmissionPipelineProvider pipeline = new ClientTransmissionPipelineProvider();

    private readonly ProfileSettingsStore settingsStore = GlobalSettingsStore.Instance.ProfileSettingsStore;
    private bool ambientCockpitEffectEnabled = true;

    private float ambientCockpitEffectVolume = 1.0f;
    private bool ambientCockpitIntercomEffectEnabled = true;

    private double lastLoaded;

    //   private readonly WaveFileWriter waveWriter;
    public ClientAudioProvider()
    {
        var radios = Constants.MAX_RADIOS;
        JitterBufferProviderInterface =
                new JitterBufferProviderInterface[radios];

        for (var i = 0; i < radios; i++)
            JitterBufferProviderInterface[i] =
                new JitterBufferProviderInterface(new WaveFormat(Constants.OUTPUT_SAMPLE_RATE, 1));
        //    waveWriter = new NAudio.Wave.WaveFileWriter($@"C:\\temp\\output{RandomFloat()}.wav", new WaveFormat(Constants.OUTPUT_SAMPLE_RATE, 1));


        ambientEffectProgress = new Dictionary<string, int>[radios];

        for (var i = 0; i < radios; i++) ambientEffectProgress[i] = new Dictionary<string, int>();
    }

    private JitterBufferProviderInterface[] JitterBufferProviderInterface { get; }

    public override int AddClientAudioSamples(ClientAudio audio)
    {
        ReLoadSettings();
        //sort out volume
        //            var timer = new Stopwatch();
        //            timer.Start();

        var newTransmission = LikelyNewTransmission();

        var floatPool = JitterBufferAudio.Pool;
        var pcmAudioFloat = floatPool.Rent(MaxSamples);

        // Target buffer contains at least one frame.
        var decodedLength = _decoder.DecodeFloat(audio.EncodedAudio, new Memory<float>(pcmAudioFloat, 0, MaxSamples), newTransmission);

        if (decodedLength <= 0)
        {
            Logger.Info("Failed to decode audio from Packet for client");
            floatPool.Return(pcmAudioFloat);
            return 0;
        }

        //convert the byte buffer to a wave buffer
        //   var waveBuffer = new WaveBuffer(tmp);

        // waveWriter.WriteSamples(tmp,0,tmp.Length);
        var
            decrytable =
                audio.Decryptable /* || (audio.Encryption == 0) <--- this test has already been performed by all callers and would require another call to check for STRICT_AUDIO_ENCRYPTION */;

        LastUpdate = DateTime.Now.Ticks;

        //return and skip jitter buffer if its passthrough as its local mic
        var jitter = new JitterBufferAudio
        {
            Audio = pcmAudioFloat,
            AudioLength = decodedLength,
            PacketNumber = audio.PacketNumber,
            Decryptable = decrytable,
            Modulation = (Modulation)audio.Modulation,
            ReceivedRadio = audio.ReceivedRadio,
            Volume = audio.Volume,
            IsSecondary = audio.IsSecondary,
            Frequency = audio.Frequency,
            NoAudioEffects = audio.NoAudioEffects,
            Guid = audio.ClientGuid,
            OriginalClientGuid = audio.OriginalClientGuid,
            Encryption = audio.Encryption,
            ReceivingPower = audio.RecevingPower,
            LineOfSightLoss = audio.LineOfSightLoss,
            Ambient = audio.Ambient.Copy(),
        };

        JitterBufferProviderInterface[audio.ReceivedRadio].AddSamples(jitter);
        return decodedLength;
    }

    //high throughput - cache these settings for 3 seconds
    private void ReLoadSettings()
    {
        var now = DateTime.Now.Ticks;
        if (now - lastLoaded > 30000000)
        {
            lastLoaded = now;
            ambientCockpitEffectEnabled =
                settingsStore.GetClientSettingBool(ProfileSettingsKeys.AmbientCockpitNoiseEffect);
            ambientCockpitEffectVolume =
                settingsStore.GetClientSettingFloat(ProfileSettingsKeys.AmbientCockpitNoiseEffectVolume);
            ambientCockpitIntercomEffectEnabled =
                settingsStore.GetClientSettingBool(ProfileSettingsKeys.AmbientCockpitIntercomNoiseEffect);
        }
    }

    // #TODO: Move to dedicated audio provider.
    private void AddCockpitAmbientAudio(int receiveRadio, Modulation modulation, Ambient ambient, Span<float> pcmAudio)
    {
        //Add cockpit effect - but not for Intercom unless you specifically opt in
        if (!ambientCockpitEffectEnabled || (modulation == Modulation.INTERCOM && !ambientCockpitIntercomEffectEnabled))
            return;

        //           clientAudio.Ambient.abType = "uh1";
        //           clientAudio.Ambient.vol = 0.35f;

        var abType = ambient?.abType;

        if (string.IsNullOrEmpty(abType)) return;

        var effect = audioEffectProvider.GetAmbientEffect(abType);

        var vol = ambient.vol;

        if (modulation == Modulation.MIDS)
            //for MIDS - half volume again - just for ambient vol
            vol = vol / 0.50f;

        var ambientEffectProg = ambientEffectProgress[receiveRadio];

        if (effect.Loaded)
        {
            var effectLength = effect.AudioEffectFloat.Length;

            if (!ambientEffectProg.TryGetValue(abType, out var progress))
            {
                progress = 0;
                ambientEffectProg[abType] = 0;
            }

            var vectorSize = Vector<float>.Count;
            var remainder = pcmAudio.Length % vectorSize;
            var limit = pcmAudio.Length - remainder;

            var effectVolume = vol * ambientCockpitEffectVolume;
            var v_effectVolume = new Vector<float>(effectVolume);


            ref float pcmAudioPtr = ref MemoryMarshal.GetReference(pcmAudio);
            ref float effectPtr = ref effect.AudioEffectFloat[0];

            if (progress + vectorSize >= effectLength)
                progress = 0;

            for (var i = 0; i < limit; i += vectorSize)
            {
                var v_samples = Vector.LoadUnsafe(ref pcmAudioPtr, (nuint)i);
                var v_effect = Vector.LoadUnsafe(ref effectPtr, (nuint)progress);

                (v_samples + v_effect * v_effectVolume).StoreUnsafe(ref pcmAudioPtr, (nuint)i);

                progress += vectorSize;
                if (progress >= effectLength)
                    progress = 0;
            }

            for (var i = limit; i < pcmAudio.Length; i++)
            {
                pcmAudio[i] += effect.AudioEffectFloat[progress] * effectVolume;

                progress++;

                if (progress >= effectLength) progress = 0;
            }

            ambientEffectProg[abType] = progress;
        }
    }

    // #TODO: Move to dedicated audio provider.
    private void AdjustVolumeForLoss(Modulation modulation, double receivingPower, float lineOfSightLoss, Span<float> pcmAudio)
    {
        if (modulation == Modulation.MIDS || modulation == Modulation.SATCOM || modulation == Modulation.INTERCOM)
            return;


        var vectorSize = Vector<float>.Count;
        var remainder = pcmAudio.Length % vectorSize;
        var limit = pcmAudio.Length - remainder;


        // https://btburnett.com/csharp/2024/12/09/using-vectorization-in-csharp-to-boost-performance#lets-do-this

        var v_powerLossFactor = Vector<float>.One; // No loss.

        //add in radio loss
        //if less than loss reduce volume
        var applyPowerLoss = receivingPower > 0.85;
        if (applyPowerLoss) // less than 20% or lower left
            v_powerLossFactor -= new Vector<float>((float)receivingPower); //gives linear signal loss from 15% down to 0%

        var v_lineOfSightLossFactor = Vector<float>.One; // No loss.

        //0 is no loss so if more than 0 reduce volume
        var applyLineOfSightLoss = lineOfSightLoss > 0;
        if (applyLineOfSightLoss)
            v_lineOfSightLossFactor -= new Vector<float>(lineOfSightLoss);

        ref float pcmAudioPtr = ref MemoryMarshal.GetReference(pcmAudio);
        
        
        for (var i = 0; i < limit; i += vectorSize)
        {
            var v_samples = Vector.LoadUnsafe(ref pcmAudioPtr, (nuint)i);
            v_samples *= v_powerLossFactor;
            v_samples *= v_lineOfSightLossFactor;

            v_samples.StoreUnsafe(ref pcmAudioPtr, (nuint)i);
        }

        for (var i = remainder; i < pcmAudio.Length; i++)
        {
            var audioFloat = pcmAudio[i];

            
            if (applyPowerLoss)
                
                audioFloat = (float)(audioFloat * (1.0f - receivingPower));

            if (applyLineOfSightLoss) audioFloat = audioFloat * (1.0f - lineOfSightLoss);

            pcmAudio[i] = audioFloat;
        }
    }

    private void AddEncryptionFailureEffect(Span<float> pcmAudio)
    {
        for (var i = 0; i < pcmAudio.Length; i++) pcmAudio[i] = RandomFloat();
    }


    private float RandomFloat()
    {
        //random float at max volume at eights
        var f = _random.Next(-32768 / 8, 32768 / 8) / (float)32768;
        if (f > 1) f = 1;
        if (f < -1) f = -1;

        return f;
    }

    public TransmissionSegment Read(int radioId, int desired)
    {
        var transmission = JitterBufferProviderInterface[radioId].Read(desired);
        if (transmission.PCMAudioLength == 0)
            return null;

        var segment = new TransmissionSegment(transmission);

        var segmentAudio = segment.AudioSpan;
        if (transmission.Decryptable)
        {
            //adjust for LOS + Distance + Volume
            AdjustVolumeForLoss(transmission.Modulation, transmission.ReceivingPower, transmission.LineOfSightLoss, segmentAudio);
            AddCockpitAmbientAudio(transmission.ReceivedRadio, transmission.Modulation, transmission.Ambient, segmentAudio);
        }
        else
        {
            AddEncryptionFailureEffect(segmentAudio);
        }

        pipeline.Process(transmission, segmentAudio);

        JitterBufferProviderInterface[radioId].Dispose(ref transmission);
        return segment;
    }
}