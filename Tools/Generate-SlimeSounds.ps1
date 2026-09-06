$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
public static class SlimeSynth {
    const int Rate = 44100;
    static double[] New(double seconds) { return new double[(int)(Rate * seconds)]; }
    static void Note(double[] samples, double start, double duration, double from, double to, double gain) {
        int offset = (int)(start * Rate), length = (int)(duration * Rate);
        double phase = 0;
        for (int i = 0; i < length && offset + i < samples.Length; i++) {
            double t = (double)i / Rate, u = (double)i / length;
            double frequency = to + (from - to) * Math.Exp(-7 * u);
            phase += 2 * Math.PI * frequency / Rate;
            double envelope = Math.Min(1, t / 0.006) * Math.Exp(-3.5 * u)
                * Math.Min(1, (length - 1 - i) / (Rate * 0.018));
            double tone = Math.Sin(phase) + 0.16 * Math.Sin(2 * phase) + 0.04 * Math.Sin(3 * phase);
            samples[offset + i] += gain * envelope * tone;
        }
    }
    static void Save(string path, double[] samples) {
        double peak = 0;
        foreach (double v in samples) peak = Math.Max(peak, Math.Abs(v));
        if (peak <= 0 || double.IsNaN(peak)) throw new Exception("Invalid audio: " + path);
        double scale = peak > 0.72 ? 0.72 / peak : 1;
        using (var writer = new BinaryWriter(File.Create(path))) {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(Rate); writer.Write(Rate * 2);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (double v in samples) writer.Write((short)Math.Round(v * scale * 32767));
        }
        Console.WriteLine(Path.GetFileName(path) + ": " + ((double)samples.Length / Rate).ToString("F2")
            + "s, peak " + (peak * scale).ToString("F3"));
    }
    public static void Generate(string folder) {
        Directory.CreateDirectory(folder);
        var s = New(0.12); Note(s, 0, 0.10, 1050, 620, 0.34); Save(Path.Combine(folder,"Click.wav"),s);
        s = New(0.40); Note(s,0,0.19,480,880,0.43); Note(s,0.12,0.25,1050,1320,0.30); Save(Path.Combine(folder,"Match.wav"),s);
        s = New(0.22); Note(s,0,0.20,310,150,0.38); Save(Path.Combine(folder,"Miss.wav"),s);
        s = New(0.60); Note(s,0,0.28,660,660,0.28); Note(s,0.13,0.28,880,880,0.28); Note(s,0.26,0.30,1320,1320,0.25); Save(Path.Combine(folder,"Hint.wav"),s);
        s = New(0.58); for(int i=0;i<7;i++) Note(s,i*0.055,0.19,360+i*100,760-i*45,0.24); Save(Path.Combine(folder,"Mix.wav"),s);
        s = New(0.27); Note(s,0,0.24,920,170,0.50); Note(s,0.025,0.17,310,100,0.16); Save(Path.Combine(folder,"Destroy.wav"),s);
        s = New(1.10); Note(s,0,0.30,660,660,0.30); Note(s,0.22,0.30,523.25,523.25,0.30); Note(s,0.44,0.60,392,261.63,0.32); Save(Path.Combine(folder,"GameOver.wav"),s);
        s = New(0.19); Note(s,0,0.16,740,740,0.30); Save(Path.Combine(folder,"Countdown.wav"),s);
        s = New(0.65); Note(s,0,0.22,523.25,523.25,0.30); Note(s,0.10,0.22,659.25,659.25,0.30); Note(s,0.20,0.40,1046.5,1046.5,0.34); Save(Path.Combine(folder,"GameStart.wav"),s);
    }
}
'@
$projectRoot = Split-Path $PSScriptRoot -Parent
[SlimeSynth]::Generate((Join-Path $projectRoot 'Assets/Sound/Generated'))
