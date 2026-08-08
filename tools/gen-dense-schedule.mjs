#!/usr/bin/env node
// Generates tests/goldens/dense-f0.schedule.json: a 2-second held AA vowel
// with an F0 event every 10 ms, the density KlattschRenderer produces.

import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const SR = 48000;
const DUR = 2000;
const STEP = 10;

// F0 = 220 Hz with 6 Hz / 30 cent vibrato plus a two-semitone rise.
const AA = { voicing: 1, F1: 700, F2: 1220, F3: 2600, BW1: 130, BW2: 70, BW3: 160, A1: 1.0, A2: 0.9, A3: 0.7 };

const schedule = [];
for (let t = 0; t < DUR; t += STEP) {
  const semis = 0.3 * Math.sin(2 * Math.PI * 6 * t / 1000) + 2.0 * (t / DUR);
  schedule.push({ atMs: t, transitionMs: STEP, target: { ...AA, F0: 220 * Math.pow(2, semis / 12) } });
}

const spec = {
  sampleRate: SR,
  samples: Math.ceil(DUR * SR / 1000),
  schedule,
};

const here = dirname(fileURLToPath(import.meta.url));
const outPath = resolve(here, '..', 'tests', 'goldens', 'dense-f0.schedule.json');
writeFileSync(outPath, JSON.stringify(spec, null, 1) + '\n');
console.log(`wrote ${outPath} (${schedule.length} events, ${spec.samples} samples)`);
