#!/usr/bin/env python3
"""
make_audio.py - procedural sound effects and music for 3D Monster Maze
======================================================================

Every sound effect and music loop in the game is synthesised from scratch by
this one script, using nothing but the Python standard library. Nothing is
sampled: each sound is a recipe that combines a handful of classic DSP
building blocks - the same ones found inside any synthesiser:

  Oscillators  Band-limited wavetable sine/saw/square/triangle with
               per-sample frequency control (vibrato, pitch sweeps). A pulse
               wave is the difference of two phase-shifted saws. A two-pole
               resonator gives exponentially decaying sine "pings" - modal
               synthesis for bells, water drops and clacking teeth.
  Noise        White noise, crackle (sparse random impulses) and smoothed
               random control signals for organic, never-quite-repeating
               wobble.
  Filters      One-pole and RBJ "cookbook" biquad low/high/band-pass filters
               whose cutoff may move over time. Moving band-pass filters are
               the vocal-tract formants that turn buzz and noise into a roar.
  Envelopes    Breakpoint curves, ADSR, percussive exponential decays and
               exponential pitch sweeps.
  Effects      tanh soft-clip saturation, a darkening feedback delay and a
               Freeverb-style reverb (parallel damped combs feeding series
               allpasses) for the stone corridors.
  Assembly     Mixing at time offsets, DC blocking, click-free fades, peak
               normalisation, and two loop tools: wrap() folds tails that
               ring past the loop end back onto the start (circular time),
               and loop_crossfade() blends the natural continuation of a
               noise bed into the head of the loop.

Music loops are built from a whole number of bars, and any sustained tone or
LFO uses a frequency with a whole number of cycles per loop, so the end of
each file flows straight back into its start.

Output is 16-bit mono 44.1 kHz WAV (mono so OpenAL can position it in 3D).
Every sound owns a fixed random seed, so re-running the script recreates
identical files.

Usage:  python3 make_audio.py
"""

import math
import os
import random
import sys
import time
import wave
from array import array

SR = 44100
TAU = 2.0 * math.pi

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO_DIR = os.path.normpath(os.path.join(
    HERE, "..", "Template", "MonsterMaze.Core", "Content", "Audio"))


# ---------------------------------------------------------------------------
# Buffers and basic arithmetic
# ---------------------------------------------------------------------------

def secs(t):
    """Seconds -> whole number of samples."""
    return int(round(t * SR))


def zeros(n):
    return array("f", bytes(4 * n))


def gain(x, g):
    return array("f", [v * g for v in x])


def mul(x, y):
    """Element-wise product (e.g. signal x envelope)."""
    return array("f", [a * b for a, b in zip(x, y)])


def peak(x):
    return max(max(x), -min(x)) if len(x) else 0.0


def norm(x):
    """Scale to a peak of 1.0 so layers can be balanced by simple gains."""
    p = peak(x)
    return gain(x, 1.0 / p) if p > 0 else array("f", x)


def layer(n, *parts):
    """Sum (signal, gain) pairs into a new buffer n samples long."""
    out = zeros(n)
    for sig, g in parts:
        m = min(n, len(sig))
        out[:m] = array("f", [a + g * b for a, b in zip(out[:m], sig)])
    return out


def mix_into(dst, src, at=0.0, g=1.0):
    """Add src into dst starting at time `at` seconds (clipped to dst)."""
    o = secs(at)
    end = min(len(dst), o + len(src))
    if end > o:
        dst[o:end] = array("f", [a + g * b for a, b in zip(dst[o:end], src)])


def fit(x, n):
    """Truncate or zero-pad to exactly n samples."""
    return x[:n] if len(x) >= n else x + zeros(n - len(x))


# ---------------------------------------------------------------------------
# Control signals: envelopes, sweeps, LFOs
# ---------------------------------------------------------------------------

def curve(points, n, smooth=False):
    """Piecewise curve through (seconds, value) breakpoints, n samples long.
    Linear segments, or S-shaped (cosine) ones if smooth=True."""
    out = zeros(n)
    pts = [(secs(t), v) for t, v in points]
    for i in range(min(pts[0][0], n)):
        out[i] = pts[0][1]
    for (ia, va), (ib, vb) in zip(pts, pts[1:]):
        span = max(ib - ia, 1)
        for i in range(max(ia, 0), min(ib, n)):
            u = (i - ia) / span
            if smooth:
                u = 0.5 - 0.5 * math.cos(math.pi * u)
            out[i] = va + (vb - va) * u
    for i in range(max(pts[-1][0], 0), n):
        out[i] = pts[-1][1]
    return out


def perc(n, attack, decay):
    """Percussive envelope: linear attack, then exponential decay with time
    constant `decay` seconds."""
    out = zeros(n)
    a = max(secs(attack), 1)
    k = math.exp(-1.0 / (decay * SR))
    level = 1.0
    for i in range(n):
        if i < a:
            out[i] = i / a
        else:
            out[i] = level
            level *= k
    return out


def adsr(n, attack, decay, sustain, hold, release):
    """ADSR envelope; the 'key' is held for `hold` seconds from the start."""
    held = max(hold, attack + decay)
    return curve([(0.0, 0.0), (attack, 1.0), (attack + decay, sustain),
                  (held, sustain), (held + release, 0.0)], n)


def sweep(f0, f1, dur, n):
    """Exponential pitch sweep from f0 to f1 Hz over `dur` s, then hold f1."""
    out = zeros(n)
    m = max(secs(dur), 1)
    ratio = f1 / f0
    for i in range(n):
        out[i] = f0 * ratio ** min(i / m, 1.0)
    return out


def lfo(n, rate, depth=1.0, offset=0.0, phase=0.0):
    """offset + depth * sin(2 pi (rate t + phase))."""
    w = TAU * rate / SR
    p = TAU * phase
    return array("f", [offset + depth * math.sin(w * i + p) for i in range(n)])


def smooth_random(n, rate, rng):
    """Random control signal in [-1, 1]: fresh random targets `rate` times a
    second joined by cosine interpolation - organic jitter and roughness."""
    step = SR / rate
    pts = [rng.uniform(-1.0, 1.0) for _ in range(int(n / step) + 3)]
    out = zeros(n)
    for i in range(n):
        p = i / step
        k = int(p)
        u = 0.5 - 0.5 * math.cos(math.pi * (p - k))
        out[i] = pts[k] + (pts[k + 1] - pts[k]) * u
    return out


def loop_freq(f, period):
    """Nudge f so it completes a whole number of cycles in `period` s."""
    return max(1, round(f * period)) / period


_NOTE_INDEX = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def note(name):
    """Equal-tempered frequency of a note name such as 'A4', 'C#5', 'Bb1'."""
    semis = _NOTE_INDEX[name[0]]
    rest = name[1:]
    while rest[0] in "#b":
        semis += 1 if rest[0] == "#" else -1
        rest = rest[1:]
    midi = semis + 12 * (int(rest) + 1)
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


# ---------------------------------------------------------------------------
# Oscillators and noise
# ---------------------------------------------------------------------------

TABLE_SIZE = 2048
_TABLES = {}


def _wavetable(shape, harmonics):
    """One cycle of a band-limited waveform built by additive synthesis."""
    key = (shape, harmonics)
    if key not in _TABLES:
        amps = []
        for k in range(1, harmonics + 1):
            if shape == "sine":
                a = 1.0 if k == 1 else 0.0
            elif shape == "saw":
                a = 1.0 / k
            elif shape == "square":
                a = 1.0 / k if k % 2 else 0.0
            else:  # triangle
                a = (1.0 if k % 4 == 1 else -1.0) / (k * k) if k % 2 else 0.0
            if a:
                amps.append((k, a))
        base = [math.sin(TAU * i / TABLE_SIZE) for i in range(TABLE_SIZE)]
        t = [sum(a * base[(k * i) % TABLE_SIZE] for k, a in amps)
             for i in range(TABLE_SIZE)]
        top = max(abs(v) for v in t)
        t = array("f", [v / top for v in t])
        t.append(t[0])  # guard point for interpolation
        _TABLES[key] = t
    return _TABLES[key]


def osc(freq, n, shape="sine", phase=0.0, max_harmonics=256):
    """Wavetable oscillator. `freq` is a number or a per-sample array (Hz).
    Harmonics are limited so the highest stays below ~20 kHz (no aliasing)."""
    const = isinstance(freq, (int, float))
    fmax = freq if const else max(freq)
    h = max(1, min(max_harmonics, int(0.45 * SR / max(fmax, 1.0))))
    h = 2 ** int(math.log2(h)) if h > 8 else h   # share tables between notes
    table = _wavetable(shape, h)
    size = TABLE_SIZE
    k = size / SR
    pos = (phase % 1.0) * size
    out = zeros(n)
    inc = freq * k if const else 0.0
    for i in range(n):
        j = int(pos)
        a = table[j]
        out[i] = a + (table[j + 1] - a) * (pos - j)
        pos += inc if const else freq[i] * k
        if pos >= size:
            pos -= size
    return out


def pulse(freq, n, width=0.5, max_harmonics=256):
    """Variable-width pulse wave = saw minus a phase-shifted saw."""
    a = osc(freq, n, "saw", 0.0, max_harmonics)
    b = osc(freq, n, "saw", width, max_harmonics)
    return array("f", [0.5 * (p - q) for p, q in zip(a, b)])


def ping(freq, decay, n, amp=1.0):
    """Exponentially decaying sine from a two-pole resonator recursion:
    y[i] = 2 r cos(w) y[i-1] - r^2 y[i-2]. Cheap modal synthesis."""
    out = zeros(n)
    w = TAU * freq / SR
    r = math.exp(-1.0 / (decay * SR))
    c1, c2 = 2.0 * r * math.cos(w), -r * r
    y2, y1 = 0.0, amp * r * math.sin(w)
    active = min(n, int(9 * decay * SR) + 2)   # stop once below -78 dB
    for i in range(1, active):
        out[i] = y1
        y1, y2 = c1 * y1 + c2 * y2, y1
    return out


def partials(freq, specs, n, attack=0.0):
    """Sum of pings: specs = [(ratio, amplitude, decay_s), ...]."""
    out = layer(n, *[(ping(freq * r, d, n), a) for r, a, d in specs
                     if freq * r < 0.45 * SR])
    if attack > 0:
        a = secs(attack)
        for i in range(min(a, n)):
            out[i] *= i / a
    return out


def white(n, rng):
    return array("f", [2.0 * rng.random() - 1.0 for _ in range(n)])


def crackle(n, rng, density):
    """Sparse random impulses (grit, gravel, crunch). `density` is the chance
    per sample of a click - a number or a per-sample array."""
    const = isinstance(density, (int, float))
    out = zeros(n)
    for i in range(n):
        if rng.random() < (density if const else density[i]):
            out[i] = rng.uniform(-1.0, 1.0)
    return out


# ---------------------------------------------------------------------------
# Filters
# ---------------------------------------------------------------------------

def _rbj(kind, f, q):
    """RBJ Audio-EQ-Cookbook biquad coefficients (normalised by a0)."""
    f = min(max(f, 5.0), 0.45 * SR)
    w = TAU * f / SR
    cs, alpha = math.cos(w), math.sin(w) / (2.0 * q)
    if kind == "lp":
        b0 = b2 = (1.0 - cs) / 2.0
        b1 = 1.0 - cs
    elif kind == "hp":
        b0 = b2 = (1.0 + cs) / 2.0
        b1 = -(1.0 + cs)
    else:  # band-pass, 0 dB peak gain
        b0, b1, b2 = alpha, 0.0, -alpha
    a0 = 1.0 + alpha
    return b0 / a0, b1 / a0, b2 / a0, -2.0 * cs / a0, (1.0 - alpha) / a0


def biquad(x, kind, freq, q=0.7071):
    """Second-order filter. `freq` is a number or a per-sample array; a moving
    cutoff is re-evaluated every 32 samples."""
    n = len(x)
    out = zeros(n)
    const = isinstance(freq, (int, float))
    block = n if const else 32
    x1 = x2 = y1 = y2 = 0.0
    for start in range(0, n, max(block, 1)):
        b0, b1, b2, a1, a2 = _rbj(kind, freq if const else freq[start], q)
        for i in range(start, min(start + block, n)):
            xi = x[i]
            yi = b0 * xi + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
            x2, x1, y2, y1 = x1, xi, y1, yi
            out[i] = yi
    return out


def lowpass(x, f, q=0.7071):
    return biquad(x, "lp", f, q)


def highpass(x, f, q=0.7071):
    return biquad(x, "hp", f, q)


def bandpass(x, f, q=1.0):
    return biquad(x, "bp", f, q)


def onepole(x, f):
    """Gentle 6 dB/octave low-pass."""
    a = math.exp(-TAU * f / SR)
    out = zeros(len(x))
    y = 0.0
    for i, v in enumerate(x):
        y = v + (y - v) * a
        out[i] = y
    return out


def dc_block(x, r=0.9995):
    """y[i] = x[i] - x[i-1] + r y[i-1]: removes DC (cutoff ~3.5 Hz)."""
    out = zeros(len(x))
    x1 = y1 = 0.0
    for i, v in enumerate(x):
        y1 = v - x1 + r * y1
        x1 = v
        out[i] = y1
    return out


# ---------------------------------------------------------------------------
# Effects
# ---------------------------------------------------------------------------

def softclip(x, drive=2.0):
    """tanh saturation, scaled so an input of +/-1 still maps to +/-1."""
    k = 1.0 / math.tanh(drive)
    return array("f", [math.tanh(drive * v) * k for v in x])


def echo(x, delay, feedback, wet=0.5, tone=3000.0, tail=None):
    """Feedback delay; each repeat is darker (low-pass inside the loop)."""
    if tail is None:
        tail = delay * math.log(0.001) / math.log(feedback)
    n = len(x) + secs(tail)
    src = fit(x, n)
    d = secs(delay)
    e = zeros(n)
    a = math.exp(-TAU * tone / SR)
    s = 0.0
    for i in range(d, n):
        v = src[i - d] + feedback * e[i - d]
        s = v + (s - v) * a
        e[i] = s
    return array("f", [p + wet * q for p, q in zip(src, e)])


COMB_DELAYS = (1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617)
ALLPASS_DELAYS = (556, 441, 341, 225)


def _comb(x, delay, feedback, damp):
    out = zeros(len(x))
    line = [0.0] * delay
    idx, store, keep = 0, 0.0, 1.0 - damp
    for i, v in enumerate(x):
        y = line[idx]
        store = y * keep + store * damp          # damping low-pass
        line[idx] = v + store * feedback
        out[i] = y
        idx += 1
        if idx == delay:
            idx = 0
    return out


def _allpass(x, delay, g=0.5):
    out = zeros(len(x))
    line = [0.0] * delay
    idx = 0
    for i, v in enumerate(x):
        b = line[idx]
        out[i] = b - v
        line[idx] = v + b * g
        idx += 1
        if idx == delay:
            idx = 0
    return out


def reverb(x, t60=1.2, wet=0.3, dry=1.0, damp=0.3, size=1.0,
           predelay=0.0, tail=None):
    """Freeverb-style mono reverb. Each comb's feedback is set from the
    requested decay time (T60) and its output scaled to roughly unit power,
    so `wet` means the same thing whatever the room size. The result is
    longer than x by `tail` seconds (default: t60)."""
    tail = t60 if tail is None else tail
    n = len(x) + secs(tail)
    src = zeros(secs(predelay)) + fit(x, n - secs(predelay))
    acc = zeros(n)
    for d0 in COMB_DELAYS:
        d = max(1, int(d0 * size))
        g = 10.0 ** (-3.0 * d / (t60 * SR))
        scale = math.sqrt(1.0 - g * g) / math.sqrt(len(COMB_DELAYS))
        y = _comb(src, d, g, damp)
        acc = array("f", [a + scale * b for a, b in zip(acc, y)])
    for d0 in ALLPASS_DELAYS:
        acc = _allpass(acc, max(1, int(d0 * size)))
    return array("f", [dry * a + wet * b for a, b in zip(fit(x, n), acc)])


# ---------------------------------------------------------------------------
# Finishing and looping
# ---------------------------------------------------------------------------

def fade(x, fade_in=0.0, fade_out=0.0):
    """Raised-cosine fades; first and last samples become exactly zero."""
    out = array("f", x)
    n = len(out)
    a, b = secs(fade_in), secs(fade_out)
    for i in range(min(a, n)):
        out[i] *= 0.5 - 0.5 * math.cos(math.pi * i / a)
    for i in range(min(b, n)):
        out[n - 1 - i] *= 0.5 - 0.5 * math.cos(math.pi * i / b)
    return out


def normalise(x, peak_db):
    return gain(x, 10.0 ** (peak_db / 20.0) / max(peak(x), 1e-12))


def finish(x, length, peak_db=-1.0, fade_in=0.002, fade_out=0.03):
    """One-shot mastering: exact length, DC removed, faded, normalised."""
    return normalise(fade(dc_block(fit(x, secs(length))), fade_in, fade_out),
                     peak_db)


def wrap(x, length):
    """Circular time: fold everything past `length` samples back onto the
    start, so notes and reverb ringing over the loop point continue into the
    head of the loop exactly as they would when it repeats."""
    out = zeros(length)
    for start in range(0, len(x), length):
        chunk = x[start:start + length]
        m = len(chunk)
        out[:m] = array("f", [a + b for a, b in zip(out[:m], chunk)])
    return out


def loop_crossfade(x, length, xfade):
    """x holds `length + xfade` samples of a continuous (noise-like) texture.
    Its overshoot - which follows on naturally from the last sample of the
    loop - is faded out over the head of the loop while the head fades in
    (equal power), so the wrap-around point is seamless."""
    out = array("f", x[:length])
    for i in range(xfade):
        u = 0.5 * math.pi * i / xfade
        out[i] = x[i] * math.sin(u) + x[length + i] * math.cos(u)
    return out


def finish_loop(x, peak_db=-3.0):
    mean = sum(x) / len(x)
    return normalise(array("f", [v - mean for v in x]), peak_db)


def write_wav(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = array("h", [int(round(max(-1.0, min(1.0, v)) * 32767.0)) for v in x])
    if sys.byteorder == "big":
        pcm.byteswap()
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


# ---------------------------------------------------------------------------
# Shared sound ingredients
# ---------------------------------------------------------------------------

def thump(n, f_start, f_end, drop, decay, attack=0.002):
    """Pitch-dropping sine hit: the heart of every thud, boom and tom."""
    return mul(osc(sweep(f_start, f_end, drop, n), n), perc(n, attack, decay))


def noise_burst(n, rng, kind, freq, q, attack, decay):
    """Filtered white noise with a percussive envelope."""
    return mul(biquad(white(n, rng), kind, freq, q), perc(n, attack, decay))


def brass(freq, dur, bright=1.0, release=0.18):
    """Synth brass: two detuned saws, delayed vibrato and a low-pass whose
    cutoff jumps open on the attack (the 'blat' of a real brass note)."""
    n = secs(dur + release)
    vib_depth = curve([(0.0, 0.0), (0.2, 0.0), (0.5, 0.006)], n)
    vib = lfo(n, 5.5)
    f = array("f", [freq * (1.0 + d * v) for d, v in zip(vib_depth, vib)])
    f2 = gain(f, 1.004)
    saws = layer(n, (osc(f, n, "saw"), 0.5), (osc(f2, n, "saw", 0.4), 0.5))
    amp = adsr(n, 0.012, 0.10, 0.72, dur, release)
    blat = perc(n, 0.015, 0.09)
    cutoff = array("f", [freq * (1.2 + bright * (6.0 * b + 2.5 * a))
                         for a, b in zip(amp, blat)])
    return mul(lowpass(saws, cutoff, 1.1), amp)


def string_section(freq, n, rng, bend=0.0):
    """Three detuned, vibrating saws - a small, harsh string section.
    `bend` is a slow pitch rise (fraction) across the note."""
    out = zeros(n)
    for detune in (-0.004, 0.0, 0.0045):
        wob = smooth_random(n, 6.0, rng)
        rise = curve([(0.0, 0.0), (n / SR, bend)], n, smooth=True)
        f = array("f", [freq * (1.0 + detune + 0.004 * w + b)
                        for w, b in zip(wob, rise)])
        out = layer(n, (out, 1.0), (osc(f, n, "saw", rng.random()), 1.0))
    return lowpass(out, 4500.0)


def music_box(freq, n):
    """Glassy music-box tine: slightly inharmonic partials, the upper ones
    dying away fast."""
    return partials(freq, [(1.0, 1.0, 1.1), (2.01, 0.30, 0.40),
                           (4.13, 0.12, 0.14), (6.27, 0.05, 0.05)], n, 0.001)


def glass(freq, n):
    """Softer, longer glass-bell voice: two slightly detuned sets of
    partials beat slowly against each other."""
    specs = [(1.0, 1.0, 1.9), (1.0035, 0.8, 1.7), (2.0, 0.15, 0.7),
             (3.0, 0.05, 0.35)]
    return partials(freq, specs, n, attack=0.03)


def heartbeat_voice():
    """'Lub-dub': two low thumps a quarter of a second apart."""
    rng = random.Random(7070)
    n = secs(0.7)
    x = zeros(n)
    m = secs(0.25)
    lub = layer(m, (thump(m, 74, 58, 0.06, 0.05, 0.010), 1.0),
                (thump(m, 150, 95, 0.02, 0.018, 0.004), 0.3),
                (noise_burst(m, rng, "lp", 140, 0.7, 0.006, 0.03), 0.25))
    dub = layer(m, (thump(m, 62, 48, 0.06, 0.042, 0.009), 1.0),
                (thump(m, 130, 85, 0.02, 0.016, 0.004), 0.25),
                (noise_burst(m, rng, "lp", 120, 0.7, 0.006, 0.025), 0.2))
    mix_into(x, lub, 0.005)
    mix_into(x, dub, 0.255, 0.62)
    return softclip(x, 2.2)   # harmonics let small speakers 'hear' the thump


# ---------------------------------------------------------------------------
# Player and world sound effects
# ---------------------------------------------------------------------------

def make_footstep(seed, thud_hz, scuff_hz, toe_time):
    rng = random.Random(seed)
    n = secs(0.22)
    heel = thump(n, thud_hz * 1.6, thud_hz, 0.035, 0.028, 0.002)
    body = noise_burst(n, rng, "lp", 320, 0.7, 0.001, 0.02)
    # Heel strike, then a softer toe scuff as the foot rolls over the stone.
    shape = curve([(0.0, 0.0), (0.004, 1.0), (0.035, 0.40),
                   (toe_time, 0.25), (toe_time + 0.012, 0.55),
                   (0.19, 0.04), (0.22, 0.0)], n)
    scuff = mul(bandpass(white(n, rng), scuff_hz, 0.8), shape)
    grit = mul(highpass(crackle(n, rng, 0.035), 2600), shape)
    x = layer(n, (norm(heel), 1.0), (norm(body), 0.45),
              (norm(scuff), 0.6), (norm(grit), 0.4))
    x = reverb(x, t60=0.3, wet=0.12, size=0.45, tail=0.0)
    return finish(x, 0.22, -1.0, fade_out=0.03)


def make_wall_bump():
    rng = random.Random(350)
    n = secs(0.35)
    thud = thump(n, 88, 46, 0.08, 0.07, 0.002)
    knock = thump(n, 220, 140, 0.02, 0.025, 0.001)
    body = noise_burst(n, rng, "lp", 600, 0.8, 0.001, 0.045)
    slap = noise_burst(n, rng, "bp", 850, 1.2, 0.001, 0.018)   # leather/cloth
    dust = mul(highpass(crackle(n, rng, curve([(0, 0.06), (0.25, 0.0)], n)),
                        2400), perc(n, 0.004, 0.07))
    x = layer(n, (norm(thud), 1.0), (norm(knock), 0.35), (norm(body), 0.7),
              (norm(slap), 0.45), (norm(dust), 0.18))
    x = reverb(softclip(x, 1.4), t60=0.4, wet=0.12, size=0.6, tail=0.0)
    return finish(x, 0.35, -1.0, fade_out=0.05)


def make_drip(seed, f0, echo_time):
    rng = random.Random(seed)
    m = secs(0.08)
    # A water drop 'plink' is a tiny resonating bubble whose pitch rises fast.
    plink = mul(osc(sweep(f0, f0 * 2.3, 0.02, m), m), perc(m, 0.0005, 0.02))
    tick = noise_burst(secs(0.01), rng, "hp", 4000, 0.7, 0.0002, 0.0015)
    x = zeros(secs(0.9))
    mix_into(x, plink)
    mix_into(x, tick, 0.0, 0.12)
    x = echo(x, echo_time, 0.35, wet=0.35, tone=2500, tail=0.0)
    x = reverb(x, t60=1.5, wet=0.45, predelay=0.015, size=1.1, tail=0.0)
    return finish(x, 0.9, -6.0, fade_in=0.001, fade_out=0.3)


def make_near_miss():
    rng = random.Random(600)
    n = secs(0.6)
    centre = curve([(0.0, 280), (0.27, 2600), (0.6, 420)], n, smooth=True)
    hiss = bandpass(white(n, rng), centre, 1.4)
    body = lowpass(white(n, rng), curve([(0, 160), (0.27, 950), (0.6, 150)],
                                        n, smooth=True), 0.8)
    shape = curve([(0.0, 0.0), (0.18, 0.55), (0.27, 1.0), (0.37, 0.55),
                   (0.6, 0.0)], n, smooth=True)
    x = mul(layer(n, (norm(hiss), 0.85), (norm(body), 0.6)), shape)
    x = reverb(x, t60=0.5, wet=0.12, size=0.7, tail=0.0)
    return finish(x, 0.6, -1.0, fade_out=0.04)


# ---------------------------------------------------------------------------
# Rex
# ---------------------------------------------------------------------------

def rex_footfall(rng, crunch_level):
    """The dry footfall shared by rex_step and rex_step_far."""
    n = secs(0.75)
    boom = thump(n, 62, 34, 0.45, 0.22, 0.004)      # sub-bass, pitch falling
    punch = thump(n, 150, 62, 0.06, 0.07, 0.002)    # so phones hear it too
    crunch = noise_burst(n, rng, "bp", 1400, 0.8, 0.0008, 0.03)
    impact = noise_burst(n, rng, "bp", 380, 1.0, 0.001, 0.06)
    earth = noise_burst(n, rng, "lp", 450, 0.7, 0.002, 0.09)
    debris = mul(bandpass(crackle(n, rng, curve([(0, 0.05), (0.5, 0.0)], n)),
                          2800, 0.9), perc(n, 0.01, 0.2))
    x = layer(n, (norm(boom), 0.85), (norm(punch), 1.0),
              (norm(crunch), crunch_level), (norm(impact), 0.7),
              (norm(earth), 0.5), (norm(debris), 0.12))
    return softclip(x, 3.0)       # saturation adds audible harmonics


def make_rex_step():
    x = rex_footfall(random.Random(900), 0.7)
    x = reverb(x, t60=0.9, wet=0.22, size=1.25, predelay=0.012)
    return finish(x, 0.9, -1.0, fade_out=0.12)


def make_rex_step_far():
    x = rex_footfall(random.Random(900), 0.12)
    x = lowpass(lowpass(x, 300), 300)          # heard through solid stone
    x = reverb(x, t60=1.5, wet=0.5, size=1.4, predelay=0.03)
    return finish(x, 1.0, -6.0, fade_out=0.15)


def roar_voice(rng, length=2.1):
    """The dry roar: a detuned buzz and gated breath noise shaped by moving
    vocal-tract formants, with roughness, tremolo and saturation."""
    n = secs(length)
    # 1. Pitch: bellows upward, strains at the top, then sags away.
    contour = curve([(0.0, 55), (0.25, 84), (0.6, 110), (1.0, 118),
                     (1.45, 104), (1.8, 80), (length, 48)], n, smooth=True)
    vibrato = lfo(n, 5.5, 0.02)
    jitter = smooth_random(n, 25.0, rng)
    f0 = array("f", [c * (1.0 + v + 0.04 * j)
                     for c, v, j in zip(contour, vibrato, jitter)])
    # 2. Sources: three detuned saws, a sub-octave pulse (period doubling -
    #    the rattle of a huge throat) and noise gated by the vocal folds.
    saws = layer(n, (osc(f0, n, "saw"), 0.4),
                 (osc(gain(f0, 1.011), n, "saw", 0.33), 0.4),
                 (osc(gain(f0, 0.986), n, "saw", 0.66), 0.4))
    sub = pulse(gain(f0, 0.5), n, 0.3)
    folds = array("f", [max(0.0, v) ** 3 for v in osc(f0, n)])
    rasp = mul(white(n, rng), array("f", [0.2 + g for g in folds]))
    source = layer(n, (saws, 1.0), (sub, 0.3), (norm(rasp), 0.9))
    rough = array("f", [1.0 + 0.45 * r for r in smooth_random(n, 45.0, rng)])
    source = mul(source, rough)
    bright = highpass(source, 250)     # tilt the buzz up before the formants
    # 3. Formants: the jaws open wide ('aaar') then close. A fixed chest
    #    resonance and a little of the raw low end give it size.
    f1 = curve([(0, 380), (0.4, 650), (1.0, 760), (1.6, 560), (length, 380)],
               n, smooth=True)
    f2 = curve([(0, 950), (0.4, 1250), (1.0, 1450), (1.6, 1100),
                (length, 850)], n, smooth=True)
    f3 = curve([(0, 2300), (1.0, 2800), (length, 2200)], n, smooth=True)
    voice = layer(n, (bandpass(bright, f1, 2.5), 1.0),
                  (bandpass(bright, f2, 4.0), 0.9),
                  (bandpass(bright, f3, 6.0), 0.5),
                  (bandpass(source, 230, 1.4), 0.7),
                  (lowpass(source, 200), 0.25))
    # 4. Shriek: a high, elephant-like overtone at the peak of the roar.
    shriek = bandpass(osc(gain(f0, 6.0), n, "saw"), 1700, 3.0)
    shriek = mul(norm(shriek), curve([(0, 0), (0.35, 0), (0.7, 1),
                                      (1.3, 0.8), (1.7, 0)], n, smooth=True))
    voice = layer(n, (norm(voice), 1.0), (shriek, 0.3))
    # 5. Loudness shape, tremolo, breath, sub weight and saturation.
    amp = curve([(0.0, 0.0), (0.06, 0.45), (0.3, 0.85), (0.8, 1.0),
                 (1.35, 0.92), (1.75, 0.6), (2.0, 0.25), (length, 0.0)],
                n, smooth=True)
    trem = lfo(n, 11.0, 0.12, 1.0)
    breath = highpass(white(n, rng), 2000)
    boom = osc(gain(f0, 0.5), n)
    x = layer(n, (norm(voice), 1.0), (norm(breath), 0.08), (boom, 0.12))
    return softclip(mul(x, mul(amp, trem)), 2.6)


def make_rex_roar():
    x = roar_voice(random.Random(2400))
    x = reverb(x, t60=1.6, wet=0.3, size=1.3, predelay=0.025)
    return finish(x, 2.4, -1.0, fade_out=0.25)


def make_rex_roar_far():
    x = roar_voice(random.Random(2400))
    x = lowpass(lowpass(x, 500), 500)
    x = reverb(x, t60=2.4, wet=0.7, dry=0.6, size=1.5, predelay=0.06)
    return finish(x, 2.6, -8.0, fade_out=0.3)


def make_rex_snort():
    rng = random.Random(800)

    def huff(length, f_hi, f_lo):
        m = secs(length)
        centre = sweep(f_hi, f_lo, length, m)
        air = white(m, rng)
        body = layer(m, (bandpass(air, centre, 1.3), 1.0),
                     (bandpass(air, gain(centre, 2.4), 3.0), 0.35),
                     (lowpass(air, 180), 0.6))
        flutter = array("f", [1.0 + 0.6 * v
                              for v in smooth_random(m, 38.0, rng)])
        grunt = lowpass(osc(sweep(52, 40, length, m), m, "saw"), 200)
        shape = curve([(0, 0), (0.012, 1.0), (0.07, 0.75),
                       (length * 0.6, 0.35), (length, 0.0)], m, smooth=True)
        return mul(layer(m, (mul(norm(body), flutter), 1.0),
                         (norm(grunt), 0.35)), shape)

    x = zeros(secs(0.8))
    mix_into(x, huff(0.28, 700, 420), 0.0, 1.0)
    mix_into(x, huff(0.36, 560, 320), 0.34, 0.85)
    x = reverb(softclip(x, 1.5), t60=0.7, wet=0.18, size=1.1, tail=0.0)
    return finish(x, 0.8, -1.0, fade_out=0.08)


def make_rex_growl():
    rng = random.Random(1600)
    length = 1.45
    n = secs(length)
    f0 = curve([(0, 42), (0.5, 50), (1.0, 47), (length, 40)], n, smooth=True)
    wander = smooth_random(n, 4.0, rng)
    f0 = array("f", [f * (1.0 + 0.06 * w) for f, w in zip(f0, wander)])
    saws = layer(n, (osc(f0, n, "saw"), 0.4),
                 (osc(gain(f0, 1.013), n, "saw", 0.5), 0.4),
                 (osc(gain(f0, 0.988), n, "saw", 0.25), 0.4))
    sub = pulse(gain(f0, 0.5), n, 0.35)
    # Vocal fry: the growl is chopped into a ragged ~20 Hz pulse train.
    fry_f = array("f", [20.0 * (1.0 + 0.15 * v)
                        for v in smooth_random(n, 6.0, rng)])
    fry = array("f", [0.25 + 0.75 * max(0.0, v) ** 4
                      for v in osc(fry_f, n)])
    rasp = bandpass(white(n, rng), 350, 1.0)
    rough = array("f", [1.0 + 0.35 * r for r in smooth_random(n, 30.0, rng)])
    src = layer(n, (saws, 1.0), (sub, 0.5), (norm(rasp), 0.9))
    src = mul(mul(src, fry), rough)
    formant = curve([(0, 260), (0.6, 330), (length, 250)], n, smooth=True)
    bright = highpass(src, 120)
    voice = layer(n, (bandpass(bright, formant, 2.0), 1.0),
                  (bandpass(bright, 650, 3.5), 0.8),
                  (bandpass(bright, 1400, 5.0), 0.2),
                  (lowpass(src, 150), 0.2))
    amp = curve([(0, 0), (0.3, 0.8), (0.7, 1.0), (1.05, 0.85),
                 (length, 0.0)], n, smooth=True)
    x = softclip(mul(norm(voice), amp), 2.2)
    x = reverb(x, t60=0.8, wet=0.15, size=1.1, tail=0.3)
    return finish(x, 1.6, -1.0, fade_out=0.1)


def make_heartbeat():
    return finish(heartbeat_voice(), 0.7, -1.0, fade_out=0.05)


def make_chomp():
    rng = random.Random(1300)
    n = secs(1.3)
    x = zeros(n + secs(0.6))

    def snap(at, level):
        m = secs(0.15)
        teeth = partials(1850, [(1.0, 1.0, 0.018), (1.57, 0.6, 0.01),
                                (0.6, 0.5, 0.03)], m)
        crack = noise_burst(m, rng, "hp", 1500, 0.7, 0.0003, 0.008)
        mix_into(x, layer(m, (norm(teeth), 0.8), (norm(crack), 1.0)),
                 at, level)
        boom = secs(0.6)
        mix_into(x, thump(boom, 110, 40, 0.2, 0.1, 0.001), at, level * 0.5)

    snap(0.0, 1.0)
    snap(0.62, 0.55)
    # Wet crunching: bursts of dense crackle plus a squelchy swept band.
    t = 0.1
    for k in range(7):
        m = secs(0.2)
        g = 0.85 * 0.82 ** k
        crunch = mul(bandpass(crackle(m, rng, 0.18), rng.uniform(900, 2200),
                              1.2), perc(m, 0.003, 0.05))
        squelch = mul(bandpass(white(m, rng), sweep(700, 250, 0.15, m), 2.0),
                      perc(m, 0.01, 0.06))
        mix_into(x, layer(m, (norm(crunch), 0.9), (norm(squelch), 0.6)),
                 t, g)
        t += 0.1 + rng.uniform(-0.02, 0.03)
    m = secs(0.5)
    mix_into(x, thump(m, 80, 42, 0.15, 0.1, 0.004), 0.16, 0.4)  # body blow
    x = reverb(softclip(x, 1.5), t60=0.6, wet=0.15, size=0.9, tail=0.0)
    return finish(x, 1.3, -1.0, fade_out=0.25)


def make_stinger_seen():
    rng = random.Random(1400)
    body_len = 1.25
    m = secs(body_len)
    # A dissonant cluster: stacked semitones and tritones.
    strings = zeros(m)
    for name in ("A2", "Bb2", "E3", "F3", "B3", "C4", "F4", "F#4"):
        strings = layer(m, (strings, 1.0),
                        (string_section(note(name), m, rng, 0.02), 1.0))
    screech = zeros(m)
    for name in ("D#5", "E5"):
        screech = layer(m, (screech, 1.0),
                        (string_section(note(name), m, rng, 0.03), 1.0))
    screech = mul(screech, lfo(m, 11.0, 0.35, 0.65))   # bowed tremolo
    shape = curve([(0.0, 0.0), (0.006, 1.0), (0.12, 0.38), (0.35, 0.45),
                   (0.95, 0.95), (1.1, 0.7), (body_len, 0.0)], m, smooth=True)
    brass_hit = layer(m, *[(brass(note(nm), 0.08, bright=2.5), 1.0)
                           for nm in ("A2", "Eb3", "A3", "Eb4")])
    hit = noise_burst(m, rng, "lp", 3000, 0.7, 0.0005, 0.05)
    sub_drop = thump(m, 100, 28, 1.0, 0.45, 0.003)
    x = layer(m, (mul(norm(strings), shape), 1.0),
              (mul(norm(screech), shape), 0.5),
              (norm(brass_hit), 1.0), (norm(hit), 0.4), (sub_drop, 0.6))
    x = reverb(softclip(x, 1.6), t60=1.3, wet=0.28, size=1.2, tail=0.2)
    return finish(x, 1.4, -1.0, fade_out=0.12)


# ---------------------------------------------------------------------------
# Jingles and interface sounds
# ---------------------------------------------------------------------------

def make_escape_fanfare():
    rng = random.Random(3000)
    x = zeros(secs(4.5))

    def stab(at, names, dur, bright=1.0):
        for nm in names:
            mix_into(x, brass(note(nm), dur, bright), at,
                     1.0 / len(names) ** 0.5)

    c_major = ("C3", "G3", "C4", "E4", "G4")
    stab(0.00, c_major, 0.13)
    stab(0.18, c_major, 0.13)
    stab(0.36, c_major, 0.30, 1.2)
    for i, nm in enumerate(("G4", "C5", "E5", "G5", "C6")):   # rising arpeggio
        mix_into(x, brass(note(nm), 0.13, 1.3), 0.72 + i * 0.075, 0.55)
    stab(1.15, ("F3", "C4", "F4", "A4", "C5"), 0.2, 1.1)
    stab(1.40, ("G3", "D4", "G4", "B4", "D5"), 0.2, 1.2)
    stab(1.65, ("C3", "G3", "C4", "E4", "G4", "C5", "E5"), 1.05, 1.3)
    # Shimmer: a bell run, a trembling high triad and random glints.
    bell_n = secs(1.2)
    for i, nm in enumerate(("C6", "E6", "G6", "C7")):
        mix_into(x, partials(note(nm), [(1, 1, 0.5), (2.0, 0.3, 0.2)], bell_n),
                 1.65 + i * 0.05, 0.12)
    pad_n = secs(1.3)
    pad = layer(pad_n, *[(osc(note(nm), pad_n), 1.0)
                         for nm in ("C7", "E7", "G7")])
    pad = mul(pad, mul(lfo(pad_n, 7.0, 0.5, 0.5),
                       curve([(0, 0), (0.3, 1), (0.8, 0.7), (1.3, 0)], pad_n,
                             smooth=True)))
    mix_into(x, pad, 1.7, 0.04)
    glints = ("C7", "E7", "G7", "C8", "E8")
    for _ in range(14):
        mix_into(x, ping(note(rng.choice(glints)), 0.12, secs(0.6)),
                 rng.uniform(1.75, 2.75), rng.uniform(0.03, 0.07))
    x = reverb(softclip(x, 1.2), t60=1.6, wet=0.25, size=1.2)
    return finish(x, 3.0, -1.0, fade_out=0.2)


def chip(freq, dur, width=0.25, release=0.04):
    """8-bit style pulse note."""
    n = secs(dur + release)
    return mul(pulse(freq, n, width, 24),
               adsr(n, 0.003, 0.05, 0.6, dur, release))


def make_new_highscore():
    rng = random.Random(1800)
    x = zeros(secs(2.2))
    for i, nm in enumerate(("C5", "E5", "G5", "C6", "E6", "G6")):
        mix_into(x, chip(note(nm), 0.06), i * 0.065, 0.85)
    mix_into(x, chip(note("G5"), 0.06), 0.42, 0.7)
    # Held chord with vibrato on top, triangle bass underneath.
    n = secs(0.95)
    wob = lfo(n, 6.5, 0.008, 1.0)
    top = mul(pulse(array("f", [note("C6") * w for w in wob]), n, 0.25, 24),
              adsr(n, 0.004, 0.1, 0.7, 0.75, 0.2))
    mix_into(x, top, 0.5, 0.5)
    for nm in ("E5", "G5"):
        mix_into(x, mul(osc(note(nm), n, "triangle"),
                        adsr(n, 0.004, 0.1, 0.7, 0.75, 0.2)), 0.5, 0.3)
    for at, nm in ((0.42, "C4"), (0.5, "C3")):
        m = secs(0.5)
        mix_into(x, mul(osc(note(nm), m, "triangle"),
                        adsr(m, 0.004, 0.08, 0.6, 0.35, 0.1)), at, 0.4)
    # Sparkle: pentatonic glints high up.
    for k in range(14):
        nm = rng.choice(("C7", "D7", "E7", "G7", "A7", "C8"))
        mix_into(x, ping(note(nm), 0.07, secs(0.4)),
                 0.5 + k * 0.075 + rng.uniform(-0.02, 0.02), 0.25 * 0.93 ** k)
    x = echo(x, 0.11, 0.3, wet=0.3, tone=5000, tail=0.0)
    return finish(x, 1.8, -3.0, fade_out=0.2)


def blip(f1, f2, split, length, shape):
    """Two-note interface blip."""
    n = secs(length)
    f = curve([(0.0, f1), (split, f1), (split + 0.002, f2)], n)
    tone = layer(n, (osc(f, n), 0.7), (osc(f, n, shape, 0.0, 5), 0.3))
    env = curve([(0, 0), (0.002, 1), (split, 0.75), (split + 0.004, 1.0),
                 (length, 0.0)], n)
    return mul(lowpass(tone, 5000), mul(env, perc(n, 0.001, length * 0.6)))


def make_ui_click():
    rng = random.Random(60)
    n = secs(0.06)
    x = layer(n, (ping(2400, 0.004, n), 1.0), (ping(5200, 0.0015, n), 0.4),
              (ping(700, 0.008, n), 0.3),
              (noise_burst(n, rng, "bp", 6000, 1.0, 0.0002, 0.0015), 0.3))
    return finish(x, 0.06, -6.0, fade_in=0.0005, fade_out=0.01)


def make_ui_select():
    return finish(blip(880.0, 1318.5, 0.07, 0.2, "square"), 0.2, -8.0,
                  fade_out=0.02)


def make_ui_back():
    return finish(blip(784.0, 523.25, 0.07, 0.2, "triangle"), 0.2, -8.0,
                  fade_out=0.02)


def make_ui_type():
    rng = random.Random(50)
    n = secs(0.05)
    x = layer(n, (noise_burst(n, rng, "bp", 3200, 1.5, 0.0002, 0.004), 1.0),
              (ping(1700, 0.006, n), 0.5), (ping(240, 0.01, n), 0.4))
    return finish(x, 0.05, -8.0, fade_in=0.0005, fade_out=0.01)


def make_score_tick():
    n = secs(0.05)
    f = curve([(0.0, 1975.5), (0.012, 1975.5), (0.013, 2637.0)], n)
    x = layer(n, (osc(f, n), 1.0), (osc(gain(f, 2.0), n), 0.15))
    x = mul(x, mul(perc(n, 0.001, 0.02),
                   curve([(0, 1), (0.012, 0.7), (0.013, 1)], n)))
    return finish(x, 0.05, -10.0, fade_in=0.0005, fade_out=0.008)


def make_countdown_beep():
    n = secs(0.15)
    x = layer(n, (osc(880.0, n), 1.0), (osc(880.0, n, "square", 0.0, 7), 0.25))
    return finish(mul(x, adsr(n, 0.003, 0.02, 0.8, 0.09, 0.05)), 0.15, -9.0,
                  fade_out=0.01)


def make_countdown_go():
    n = secs(0.35)
    f = sweep(1700.0, 1760.0, 0.02, n)
    x = layer(n, (osc(f, n), 0.6), (osc(f, n, "square", 0.0, 9), 0.25),
              (osc(gain(f, 1.003), n, "square", 0.3, 9), 0.25))
    return finish(mul(x, adsr(n, 0.003, 0.05, 0.75, 0.22, 0.12)), 0.35, -7.0,
                  fade_out=0.02)


# ---------------------------------------------------------------------------
# Music loops
# ---------------------------------------------------------------------------

def make_ambient_loop():
    rng = random.Random(9001)
    period = 12.0
    L, pre, X = secs(period), secs(1.0), secs(2.0)
    n = pre + L + X

    def lf(f):
        return loop_freq(f, period)

    # Drone: detuned low sines beating slowly, plus a dark filtered saw.
    # All frequencies complete whole cycles in 12 s, so the drone is periodic.
    drone = layer(n, (osc(lf(55.0), n), 1.0), (osc(lf(55.25), n), 1.0),
                  (osc(lf(82.5), n), 0.22), (osc(lf(110.08), n), 0.12),
                  (lowpass(osc(lf(41.25), n, "saw"), 110.0), 0.5),
                  (lowpass(osc(lf(55.08), n, "saw"), 320.0), 0.3))
    drone = mul(drone, lfo(n, 1.0 / period, 0.2, 0.8))
    drone = drone[pre:pre + L]
    # Wind: band-passed noise whose pitch and level swell slowly.
    centre = array("f", [a + b for a, b in zip(lfo(n, 1 / 12.0, 250, 600),
                                               lfo(n, 1 / 4.0, 80))])
    wind = layer(n, (bandpass(white(n, rng), centre, 0.9), 1.0),
                 (bandpass(white(n, rng), lfo(n, 1 / 6.0, 220, 1300), 7.0),
                  0.5))
    swell = mul(lfo(n, 1 / 12.0, 0.45, 0.55, 0.3), lfo(n, 1 / 3.0, 0.15, 1.0))
    wind = loop_crossfade(mul(wind, swell)[pre:], L, X)
    # Two very soft, distant rumbles.
    events = zeros(L + secs(4.0))
    for at in (3.6, 9.2):
        m = secs(2.6)
        rumble = layer(m, (lowpass(lowpass(white(m, rng), 90), 90), 1.0),
                       (osc(sweep(40, 32, 2.6, m), m), 0.3))
        rumble = mul(norm(rumble), curve([(0, 0), (0.8, 1), (2.6, 0)], m,
                                         smooth=True))
        mix_into(events, rumble, at)
    events = wrap(events, L)
    bed = layer(L, (norm(drone), 0.55), (norm(wind), 0.4), (events, 0.25))
    send = layer(L, (norm(wind), 0.4), (events, 0.25))
    rev = wrap(reverb(send, t60=3.0, wet=0.6, dry=0.0, size=1.6,
                      predelay=0.04, tail=3.5), L)
    return finish_loop(layer(L, (bed, 1.0), (rev, 1.0)), -3.0)


def bass_pluck(freq, dur, accent):
    n = secs(dur + 0.08)
    src = layer(n, (osc(freq, n, "saw"), 0.7),
                (osc(freq * 0.5, n, "square"), 0.3))
    cutoff = array("f", [freq * 2.0 + (900.0 + 700.0 * accent) * e
                         for e in perc(n, 0.002, 0.06)])
    return mul(lowpass(src, cutoff, 2.0), adsr(n, 0.003, 0.06, 0.3, dur, 0.04))


def tom(freq, level):
    n = secs(0.35)
    rng = random.Random(int(freq))
    body = thump(n, freq * 1.6, freq, 0.05, 0.09, 0.001)
    skin = noise_burst(n, rng, "lp", 1200, 0.7, 0.0005, 0.015)
    return layer(n, (body, level), (skin, 0.3 * level))


def make_tension_loop():
    rng = random.Random(120)
    period = 8.0                      # 120 BPM: 4 bars of 4/4
    beat = 0.5
    L = secs(period)
    tail = secs(2.5)
    bass = zeros(L + tail)
    drums = zeros(L + tail)
    riser = zeros(L + tail)
    bars = (("D2", "D2", "D3", "D2", "D2", "D2", "D3", "D2"),
            ("D2", "D2", "D3", "D2", "D2", "F2", "E2", "D2"),
            ("Bb1", "Bb1", "Bb2", "Bb1", "Bb1", "Bb1", "Bb2", "Bb1"),
            ("A1", "A1", "A2", "A1", "A1", "A1", "A2", "C#2"))
    for b, notes in enumerate(bars):
        for i, nm in enumerate(notes):
            at = b * 4 * beat + i * beat / 2
            accent = 1.0 if i % 2 == 0 else 0.0
            mix_into(bass, bass_pluck(note(nm), 0.14, accent), at,
                     0.8 + 0.2 * accent)
        for k in range(4):
            if b == 3 and k == 3:
                continue
            mix_into(drums, tom(92.0, 1.0 if k == 0 else 0.7),
                     b * 4 * beat + k * beat)
    for i, f in enumerate((100.0, 115.0, 130.0, 150.0)):    # fill into bar 1
        mix_into(drums, tom(f, 0.45 + 0.1 * i), 7.5 + i * 0.125)
    # Rising dissonant tone in the last bar: a minor second gliding upward.
    m = secs(2.08)
    shape = curve([(0, 0), (1.0, 0.25), (1.8, 0.8), (2.0, 1.0), (2.08, 0.0)],
                  m, smooth=True)
    trem = array("f", [1.0 - 0.3 * (0.5 + 0.5 * v)
                       for v in osc(sweep(6.0, 14.0, 2.0, m), m)])
    for nm in ("D6", "Eb6"):
        f = sweep(note(nm), note(nm) * 2 ** (3 / 12), 2.0, m)
        tone = layer(m, (osc(f, m), 0.7), (osc(f, m, "triangle", 0.25), 0.3))
        mix_into(riser, mul(tone, mul(shape, trem)), 6.0, 0.5)

    def lf(f):
        return loop_freq(f, period)

    # Sustained, barely-there high minor second and low D pedal (periodic).
    hold = layer(L, (osc(lf(note("A5")), L), 0.5),
                 (osc(lf(note("Bb5")), L), 0.5),
                 (osc(lf(note("D1")), L), 1.2))
    bass, drums, riser = wrap(bass, L), wrap(drums, L), wrap(riser, L)
    mixed = layer(L, (softclip(norm(bass), 1.5), 0.45), (norm(drums), 0.8),
                  (riser, 0.22), (hold, 0.06))
    send = layer(L, (norm(drums), 0.25), (riser, 0.35), (norm(bass), 0.08))
    rev = wrap(reverb(send, t60=1.8, wet=0.6, dry=0.0, size=1.3, tail=2.5), L)
    return finish_loop(layer(L, (mixed, 1.0), (rev, 1.0)), -3.0)


def pad_chord(names, dur, rng):
    """Soft pad: three detuned saws per note, low-passed, slow in and out."""
    n = secs(dur + 1.2)
    out = zeros(n)
    for nm in names:
        for cents in (-7.0, 0.0, 6.0):
            f = note(nm) * 2 ** (cents / 1200.0)
            out = layer(n, (out, 1.0), (osc(f, n, "saw", rng.random()), 1.0))
    return mul(lowpass(out, 750.0), adsr(n, 0.7, 0.3, 0.85, dur, 1.2))


def make_title_music():
    rng = random.Random(90)
    beat = 60.0 / 90.0               # 90 BPM
    bar = 4 * beat                   # 6 bars = exactly 16 s
    period = 6 * bar
    L = secs(period)
    tail = secs(3.5)
    bells = zeros(L + tail)
    melody = zeros(L + tail)
    pads = zeros(L + tail)
    heart = zeros(L + tail)
    chords = (  # arpeggio notes (root, 5th, octave, 10th, 12th), pad notes
        (("A4", "E5", "A5", "C6", "E6"), ("A3", "C4", "E4")),
        (("F4", "C5", "F5", "A5", "C6"), ("F3", "A3", "C4")),
        (("D4", "A4", "D5", "F5", "A5"), ("D3", "F3", "A3")),
        (("E4", "B4", "E5", "G#5", "B5"), ("E3", "G#3", "B3")),
        (("A4", "E5", "A5", "C6", "E6"), ("A3", "C4", "E4")),
        (("E4", "B4", "E5", "G#5", "B5"), ("E3", "G#3", "D4")))
    tune = (  # (bar, beat, note, beats)
        (0, 0, "E6", 3), (0, 3, "D6", 1), (1, 0, "C6", 3), (1, 3, "A5", 1),
        (2, 0, "D6", 2), (2, 2, "F6", 2), (3, 0, "E6", 3), (3, 3, "G#5", 1),
        (4, 0, "A5", 2), (4, 2, "C6", 1), (4, 3, "E6", 1),
        (5, 0, "D6", 2), (5, 2, "B5", 1), (5, 3, "G#5", 1))
    bell_n = secs(1.6)
    thump_thump = heartbeat_voice()
    for b, (arp, pad) in enumerate(chords):
        for i, idx in enumerate((0, 1, 2, 3, 4, 3, 2, 1)):
            detune = 2 ** (rng.uniform(-6, 6) / 1200.0)   # an old music box
            vel = 0.9 if i % 2 == 0 else 0.65
            mix_into(bells, music_box(note(arp[idx]) * detune, bell_n),
                     b * bar + i * beat / 2, vel)
        mix_into(pads, pad_chord(pad, bar, rng), b * bar)
        for k in (0, 2):               # slow heartbeat on beats 1 and 3
            mix_into(heart, thump_thump, b * bar + k * beat)
    for b, k, nm, beats in tune:
        m = secs(beats * beat + 1.5)
        mix_into(melody, glass(note(nm), m), b * bar + k * beat)
    bells, melody = wrap(bells, L), wrap(melody, L)
    pads, heart = wrap(pads, L), wrap(onepole(heart, 150), L)
    mixed = layer(L, (norm(bells), 0.5), (norm(melody), 0.45),
                  (norm(pads), 0.3), (norm(heart), 0.35))
    send = layer(L, (norm(bells), 0.5), (norm(melody), 0.6),
                 (norm(pads), 0.25))
    rev = wrap(reverb(send, t60=2.6, wet=0.45, dry=0.0, size=1.4,
                      predelay=0.03, tail=3.5), L)
    return finish_loop(layer(L, (mixed, 1.0), (rev, 1.0)), -3.0)


def make_exit_hum():
    rng = random.Random(4000)
    period = 4.0
    L, pre, X = secs(period), secs(0.5), secs(1.0)
    n = pre + L + X

    def lf(f):
        return loop_freq(f, period)

    # Airy E major pad: each note is three detuned voices (a natural chorus).
    # Every frequency and LFO rate completes whole cycles in 4 s.
    pad = zeros(n)
    for i, nm in enumerate(("E4", "G#4", "B4", "E5", "G#5", "B5")):
        f = lf(note(nm))
        voices = layer(n, *[(osc(f + d, n, "triangle", rng.random()), g)
                            for d, g in ((-0.5, 0.45), (0.0, 1.0),
                                         (0.25, 0.6))])
        shimmer = lfo(n, (1 + i % 3) / period, 0.15, 0.85, rng.random())
        pad = layer(n, (pad, 1.0), (mul(voices, shimmer), 1.0 / (1 + 0.3 * i)))
    sparkle = layer(n, *[(osc(lf(note(nm)) + d, n), 1.0)
                         for nm in ("E6", "B6", "E7") for d in (0.0, 0.25)])
    sparkle = mul(sparkle, lfo(n, 2.0 / period, 0.5, 0.5))
    tonal = layer(n, (lowpass(pad, 3500.0), 1.0), (norm(sparkle), 0.08))
    tonal = tonal[pre:pre + L]
    air = mul(bandpass(highpass(white(n, rng), 2500.0), 6000.0, 0.7),
              lfo(n, 1.0 / period, 0.3, 0.7))
    air = loop_crossfade(air[pre:], L, X)
    body = layer(L, (norm(tonal), 0.8), (norm(air), 0.12))
    rev = wrap(reverb(body, t60=2.0, wet=0.3, dry=0.0, size=1.2, tail=2.5), L)
    return finish_loop(layer(L, (body, 1.0), (rev, 1.0)), -3.0)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

JOBS = [
    ("Sfx/footstep_1.wav", lambda: make_footstep(101, 88.0, 2100.0, 0.085)),
    ("Sfx/footstep_2.wav", lambda: make_footstep(102, 96.0, 2600.0, 0.075)),
    ("Sfx/footstep_3.wav", lambda: make_footstep(103, 80.0, 1850.0, 0.095)),
    ("Sfx/footstep_4.wav", lambda: make_footstep(104, 92.0, 2350.0, 0.080)),
    ("Sfx/wall_bump.wav", make_wall_bump),
    ("Sfx/rex_step.wav", make_rex_step),
    ("Sfx/rex_step_far.wav", make_rex_step_far),
    ("Sfx/rex_roar.wav", make_rex_roar),
    ("Sfx/rex_roar_far.wav", make_rex_roar_far),
    ("Sfx/rex_snort.wav", make_rex_snort),
    ("Sfx/rex_growl.wav", make_rex_growl),
    ("Sfx/heartbeat.wav", make_heartbeat),
    ("Sfx/drip_1.wav", lambda: make_drip(201, 850.0, 0.13)),
    ("Sfx/drip_2.wav", lambda: make_drip(202, 1150.0, 0.17)),
    ("Sfx/drip_3.wav", lambda: make_drip(203, 1500.0, 0.11)),
    ("Sfx/chomp.wav", make_chomp),
    ("Sfx/escape_fanfare.wav", make_escape_fanfare),
    ("Sfx/stinger_seen.wav", make_stinger_seen),
    ("Sfx/near_miss.wav", make_near_miss),
    ("Sfx/ui_click.wav", make_ui_click),
    ("Sfx/ui_select.wav", make_ui_select),
    ("Sfx/ui_back.wav", make_ui_back),
    ("Sfx/ui_type.wav", make_ui_type),
    ("Sfx/score_tick.wav", make_score_tick),
    ("Sfx/new_highscore.wav", make_new_highscore),
    ("Sfx/countdown_beep.wav", make_countdown_beep),
    ("Sfx/countdown_go.wav", make_countdown_go),
    ("Music/ambient_loop.wav", make_ambient_loop),
    ("Music/tension_loop.wav", make_tension_loop),
    ("Music/title_music.wav", make_title_music),
    ("Music/exit_hum.wav", make_exit_hum),
]


def main():
    started = time.time()
    print("Writing audio to", AUDIO_DIR)
    print("%-26s %8s %9s %7s" % ("file", "seconds", "peak dB", "render"))
    for rel, make in JOBS:
        t0 = time.time()
        x = make()
        write_wav(os.path.join(AUDIO_DIR, rel), x)
        p = 20.0 * math.log10(max(peak(x), 1e-9))
        print("%-26s %8.3f %9.2f %6.1fs"
              % (rel, len(x) / SR, p, time.time() - t0))
    print("Done: %d files in %.1f s" % (len(JOBS), time.time() - started))


if __name__ == "__main__":
    main()
