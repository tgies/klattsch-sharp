#!/usr/bin/env node
// The JS render loop forced to float32 via Math.fround at every step. Output
// matching this but not plain JS points at single precision, not port logic.
//
//   cd tools && npm install && node f32-emulate.mjs

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

import { compileString } from 'klattsch';
import { BandpassBiquad, glottalPulse, xorshift, softClip } from 'klattsch/engine/dsp.js';

const f = Math.fround;
const here = dirname(fileURLToPath(import.meta.url));
const goldensDir = resolve(here, '..', 'tests', 'goldens');
const outDir = resolve(here, 'out', 'f32-emulated');
mkdirSync(outDir, { recursive: true });

const PARAMS = ['F0','voicing','F1','BW1','A1','F2','BW2','A2','F3','BW3','A3','gain',
  'vibratoDepth','vibratoRate','tremoloDepth','tremoloRate','aspiration','tilt','effort'];
const DEFAULT = { F0:120, voicing:0, F1:500, BW1:80, A1:0, F2:1500, BW2:120, A2:0,
  F3:2500, BW3:160, A3:0, gain:3.5, vibratoDepth:0, vibratoRate:5,
  tremoloDepth:0, tremoloRate:5, aspiration:0, tilt:0, effort:0.5 };

function render(schedule, sr, n) {
  const cur = {}, tgt = {}, inc = {};
  for (const k of PARAMS) { cur[k] = f(DEFAULT[k]); tgt[k] = f(DEFAULT[k]); inc[k] = 0; }
  const sched = schedule.map(e => ({
    atSample: Math.floor((e.atMs ?? 0) * sr / 1000),
    target: e.target,
    transitionSamples: Math.max(1, Math.floor((e.transitionMs ?? 30) * sr / 1000)),
  }));
  let idx = 0, counter = 0, trans = 0;
  let gPhase = 0, vPhase = 0, tPhase = 0, tiltPrev = f(0), lfsr = 0xACE1ACE1 | 0;
  const bp1 = new BandpassBiquad(), bp2 = new BandpassBiquad(), bp3 = new BandpassBiquad();
  const out = new Float32Array(n);

  for (let i = 0; i < n; i++) {
    while (idx < sched.length && sched[idx].atSample <= counter) {
      const evt = sched[idx++];
      trans = evt.transitionSamples;
      const N = f(trans);
      for (const k of PARAMS) {
        if (k in evt.target) tgt[k] = f(evt.target[k]);
        inc[k] = f(f(tgt[k] - cur[k]) / N);
      }
    }
    counter++;

    if (trans > 0) {
      for (const k of PARAMS) cur[k] = f(cur[k] + inc[k]);
      trans--;
      if (trans === 0) for (const k of PARAMS) cur[k] = tgt[k];
    }

    vPhase += 2 * Math.PI * cur.vibratoRate / sr;
    vPhase -= 2 * Math.PI * Math.floor(vPhase / (2 * Math.PI));
    const effF0 = f(cur.F0 + f(cur.vibratoDepth * f(Math.sin(vPhase))));

    tPhase += 2 * Math.PI * cur.tremoloRate / sr;
    tPhase -= 2 * Math.PI * Math.floor(tPhase / (2 * Math.PI));
    const tremoloMod = f(1 - f(cur.tremoloDepth * f(f(0.5) + f(f(0.5) * f(Math.sin(tPhase))))));

    const v = cur.voicing < 0 ? f(0) : cur.voicing > 1 ? f(1) : cur.voicing;
    lfsr = xorshift(lfsr);
    const noiseSample = f(f(lfsr) / f(2147483648));   // single-precision division
    const pulseVal = f(glottalPulse(gPhase, cur.effort));
    const voicedGain = f(1 - f(cur.aspiration * f(0.85)));
    const exc = f(f(f(v * pulseVal) * voicedGain)
               + f(f(f(1 - v) * noiseSample) * f(0.35))
               + f(f(cur.aspiration * noiseSample) * f(0.5)));
    gPhase += effF0 / sr;
    gPhase -= Math.floor(gPhase);

    bp1.setFreq(cur.F1, cur.BW1, sr);
    bp2.setFreq(cur.F2, cur.BW2, sr);
    bp3.setFreq(cur.F3, cur.BW3, sr);

    const y = f(f(f(f(f(bp1.process(exc)) * cur.A1)
                 + f(f(bp2.process(exc)) * cur.A2))
                 + f(f(bp3.process(exc)) * cur.A3))
                 * cur.gain);
    const y2 = f(y * tremoloMod);
    const tilted = f(y2 - f(cur.tilt * tiltPrev));
    tiltPrev = y2;
    out[i] = f(softClip(tilted));
  }
  return out;
}

const manifest = JSON.parse(readFileSync(resolve(goldensDir, 'manifest.json'), 'utf8'));
let count = 0;
for (const entry of manifest) {
  if (entry.text === undefined) continue;
  const { name, text, sampleRate: SR } = entry;
  const { schedule, totalMs } = compileString(text);
  const n = Math.max(1, Math.ceil(totalMs * SR / 1000));
  writeFileSync(resolve(outDir, `${name}.f32`), Buffer.from(render(schedule, SR, n).buffer));
  count++;
}
console.log(`wrote ${count} float32-emulated renders to ${outDir}`);
