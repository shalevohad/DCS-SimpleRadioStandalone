﻿using System;
using System.Text.RegularExpressions;

namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Models.Player;

public partial class RadioBase
{
    private string _name = "";
    public bool enc; // encryption enabled
    public byte encKey;
    public double freq = 1;
    public Modulation modulation = Modulation.DISABLED;

    //should the radio restransmit?
    public bool retransmit = false;
    public double secFreq = 1;
    
    private string _model = "";

    // Radio model, lowercase alphanumeric only (arc123, r456, etc).
    public string Model
    {
        get => _model;
        set { 
            value ??= "";

            value = value.ToLowerInvariant().Trim();

            value = NormaliseRadioRegex().Replace(value, "");
            if (value.Length > 32)
            {
                value = value.Substring(0, 32);
            }
            _model = value;
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            value ??= "";

            value = value.ToLowerInvariant().Trim();

            value = NormaliseRadioRegex().Replace(value, "");
            
            if (value.Length > 32)
            {
                value = value.Substring(0, 32);
            }
            _name = value;
        }
    }


    /**
     * Used to determine if we should send an update to the server or not
     * We only need to do that if something that would stop us Receiving happens which
     * is frequencies and modulation
     */
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var compare = (RadioBase)obj;

        if (!FreqCloseEnough(freq, compare.freq)) return false;
        if (modulation != compare.modulation) return false;
        if (enc != compare.enc) return false;
        if (encKey != compare.encKey) return false;
        if (retransmit != compare.retransmit) return false;
        if (!FreqCloseEnough(secFreq, compare.secFreq)) return false;
        if (Model != compare?.Model) return false;

        return true;
    }

    public override int GetHashCode() => HashCode.Combine(freq, modulation, enc, encKey, retransmit, secFreq, Name);

    //comparing doubles is risky - check that we're close enough to hear (within 100hz)

    public static bool FreqCloseEnough(double freq1, double freq2)
    {
        var diff = Math.Abs(freq1 - freq2);

        return diff < 500;
    }

    public RadioBase DeepClone()
    {
        return new RadioBase
        {
            enc = enc,
            modulation = modulation,
            secFreq = secFreq,
            encKey = encKey,
            freq = freq,
            Model = Model
        };
    }


    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex NormaliseRadioRegex();
}