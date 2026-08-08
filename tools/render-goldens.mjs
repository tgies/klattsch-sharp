#!/usr/bin/env node
// Renders the golden WAV corpus from the pinned npm `klattsch` into
// ../tests/goldens/, overwriting the .wav files in place.
//
//   cd tools && npm install && npm run goldens

import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

import { compileString, FormantSynth, encodeWav } from 'klattsch';

const here = dirname(fileURLToPath(import.meta.url));
const goldensDir = resolve(here, '..', 'tests', 'goldens');
const manifest = JSON.parse(readFileSync(resolve(goldensDir, 'manifest.json'), 'utf8'));

for (const entry of manifest) {
  const { name, sampleRate: SR } = entry;
  // One part per [voice=N] section; a schedule case is a single part.
  let parts;
  let totalSamples;

  if (entry.text !== undefined) {
    const opts = entry.bank ? { bank: entry.bank } : {};
    const { voices, totalMs, warnings } = compileString(entry.text, opts);
    if (warnings.length) {
      console.error(`[${name}] warnings: ${warnings.join(', ')}`);
    }
    totalSamples = Math.max(1, Math.ceil(totalMs * SR / 1000));
    parts = voices.map(v => ({
      schedule: v.schedule,
      samples: Math.max(1, Math.ceil(v.totalMs * SR / 1000)),
    }));
  } else if (entry.scheduleFile !== undefined) {
    const spec = JSON.parse(readFileSync(resolve(goldensDir, entry.scheduleFile), 'utf8'));
    totalSamples = spec.samples;
    parts = [{ schedule: spec.schedule, samples: spec.samples }];
  } else {
    throw new Error(`[${name}] manifest entry has neither "text" nor "scheduleFile"`);
  }

  // Voices are simultaneous: render each into its own buffer and sum.
  const buf = new Float32Array(totalSamples);
  for (const part of parts) {
    if (!part.schedule.length) continue;
    const vb = new Float32Array(part.samples);
    new FormantSynth({ sampleRate: SR, schedule: part.schedule }).process(vb);
    const n = Math.min(buf.length, vb.length);
    for (let i = 0; i < n; i++) buf[i] += vb[i];
  }

  const { bytes } = encodeWav(buf, SR, { peakNormalize: entry.peakNormalize ?? 0 });
  const path = resolve(goldensDir, `${name}.wav`);
  writeFileSync(path, bytes);
  console.log(`wrote ${path} (${(bytes.length / 1024).toFixed(0)} KB, ${totalSamples} samples)`);
}
