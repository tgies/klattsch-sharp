# Golden WAV corpus

Reference WAVs for `KlattschSharp.Tests.Parity.GoldenParityTests`, rendered
from the npm `klattsch` package pinned in `tools/package.json` against the
inputs in `manifest.json`.

Goldens are regenerated from the JS reference only; the Rust port is not
authoritative.

Manifest entries are either text cases (compiled with `compileString`) or
schedule cases (a committed `*.schedule.json` driving `FormantSynth`
directly). Entries with `peakNormalize` render through the encoder's gain
stage; the rest render raw.

## Regenerating

    cd tools
    npm install
    npm run goldens

This overwrites the `.wav` files in place. Committing changed goldens means
accepting a behavior change in the JS reference, or a version bump of the pin
in `tools/package.json`.

## Exactness

Every i16 sample must match the reference exactly. The port's render is
encoded through `KlattschSharp.Wav.WavWriter` at the entry's `peakNormalize`
setting before the comparison, so the encoder's own quantization is part of
what the corpus pins.

Trigonometry is the one place output could drift without anything being
wrong: .NET takes `Math.Sin` and `Math.Cos` from the platform C runtime, Node
uses V8's, and they can disagree by an ulp. A failure whose worst deviation
is one i16 step is that; anything larger is a divergence in the port.
