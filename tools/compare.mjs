#!/usr/bin/env node
// Renders every manifest text case from both the pinned npm `klattsch` and
// the KlattschSharp CLI and reports how many i16 samples differ.
//
//   cd tools && npm install && npm run compare

import { execFileSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

import { compileString, FormantSynth, encodeWav } from 'klattsch';

// The CLI only exposes normalization on/off at the encoder default
const CLI_PEAK_NORMALIZE = 0.95;

const here = dirname(fileURLToPath(import.meta.url));
const goldensDir = resolve(here, '..', 'tests', 'goldens');
const cliProject = resolve(here, '..', 'src', 'KlattschSharp.Cli');
const manifest = JSON.parse(readFileSync(resolve(goldensDir, 'manifest.json'), 'utf8'));

function readWavSamples(bytes) {
  let off = 12;
  while (off + 8 <= bytes.length) {
    const id = bytes.toString('ascii', off, off + 4);
    const size = bytes.readInt32LE(off + 4);
    if (id === 'data') {
      const n = size / 2;
      const out = new Int16Array(n);
      for (let i = 0; i < n; i++) out[i] = bytes.readInt16LE(off + 8 + i * 2);
      return out;
    }
    off += 8 + size + (size & 1);
  }
  throw new Error('no data chunk');
}

function renderJs(text, SR, bank, peakNormalize) {
  const { voices, totalMs } = compileString(text, bank ? { bank } : {});
  const buf = new Float32Array(Math.max(1, Math.ceil(totalMs * SR / 1000)));
  for (const v of voices) {
    if (!v.schedule.length) continue;
    const vb = new Float32Array(Math.max(1, Math.ceil(v.totalMs * SR / 1000)));
    new FormantSynth({ sampleRate: SR, schedule: v.schedule }).process(vb);
    const n = Math.min(buf.length, vb.length);
    for (let i = 0; i < n; i++) buf[i] += vb[i];
  }
  const { bytes } = encodeWav(buf, SR, { peakNormalize });
  return readWavSamples(Buffer.from(bytes));
}

const work = mkdtempSync(join(tmpdir(), 'klattsch-compare-'));
let worstStep = 0;
let failures = 0;
let skipped = 0;

try {
  for (const entry of manifest) {
    const { name, text, sampleRate: SR, bank } = entry;
    const peakNormalize = entry.peakNormalize ?? 0;

    if (text === undefined) {
      console.log(`${name.padEnd(24)} skipped (schedule case; covered by GoldenParityTests)`);
      skipped++;
      continue;
    }
    if (peakNormalize !== 0 && peakNormalize !== CLI_PEAK_NORMALIZE) {
      console.log(`${name.padEnd(24)} skipped (peakNormalize=${peakNormalize} is not reachable through the CLI)`);
      skipped++;
      continue;
    }

    const inPath = join(work, `${name}.klatt`);
    const outPath = join(work, `${name}.wav`);
    writeFileSync(inPath, text + '\n');
    const cliArgs = [
      'run', '--project', cliProject, '-c', 'Release', '--',
      inPath, outPath, '--sample-rate', String(SR),
    ];
    if (peakNormalize === 0) cliArgs.push('--no-normalize');
    if (bank) cliArgs.push('--bank', bank);
    execFileSync('dotnet', cliArgs, { stdio: ['ignore', 'ignore', 'inherit'] });

    const js = renderJs(text, SR, bank, peakNormalize);
    const cs = readWavSamples(readFileSync(outPath));

    if (Math.abs(js.length - cs.length) > 1) {
      console.error(`${name}: length mismatch js=${js.length} cs=${cs.length}`);
      failures++;
    }

    const n = Math.min(js.length, cs.length);
    let diff = 0;
    let maxStep = 0;
    for (let i = 0; i < n; i++) {
      const step = Math.abs(js[i] - cs[i]);
      if (step === 0) continue;
      diff++;
      if (step > maxStep) maxStep = step;
    }
    if (maxStep > worstStep) worstStep = maxStep;
    if (diff !== 0) failures++;
    console.log(`${name.padEnd(24)} diff=${diff}/${n} maxStep=${maxStep} ${diff === 0 ? 'ok' : 'FAIL'}`);
  }
} finally {
  rmSync(work, { recursive: true, force: true });
}

console.log(`\n${skipped} skipped, worst deviation ${worstStep} i16 step(s) -> ${failures === 0 ? 'PASS' : 'FAIL'}`);
process.exit(failures === 0 ? 0 : 1);
