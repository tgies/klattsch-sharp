# KlattschSharp

A .NET port of [klattsch](https://github.com/tgies/klattsch), a parallel-formant
speech synthesizer.

## Install

```sh
dotnet add package KlattschSharp.Core
dotnet add package KlattschSharp.Wav
dotnet add package KlattschSharp.Text
```

## Minimal example

```csharp
using KlattschSharp;
using KlattschSharp.Text;
using KlattschSharp.Wav;

var result = Compiler.Compile("HH AH L OW", new CompileOptions { SampleRate = 48_000 });
int total = (int)Math.Ceiling(result.TotalMs * 48_000 / 1000.0);
var buf = new float[total];

var synth = new FormantSynth(48_000);
synth.QueueSchedule(result.Schedule);
synth.Process(buf, 0, buf.Length);

byte[] wav = WavWriter.Encode(buf, 48_000);
File.WriteAllBytes("hello.wav", wav);
```

## License

MIT.
