"""Generate the approved welcome in short, tender takes with the installed model."""
import hashlib
import json
import os
from pathlib import Path
import runpy
import sys

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "output/HandsStartupNatural_20261028"
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
sys.dont_write_bytecode = True

def main():
    import numpy as np
    import soundfile as sf
    from huggingface_hub import snapshot_download
    from kittenml import KittenTTS

    if (OUTPUT / "01_welcome.wav").exists():
        raise SystemExit("Output already exists; preserve it before generating another take.")
    entries = json.loads((ROOT / "Docs/GronnyHandsStartupDialogues.json").read_text(encoding="utf-8-sig"))
    text = entries[0]["text"]
    parts = [
        "Hi! I'm Gronny. If you're using hand tracking, hold both hands comfortably in front of you where you can see them.",
        "Today I'll help you grow your own tomato. We'll learn about sustainable gardening by giving our plants just the water they need.",
        "We'll practise together first, then use a limited water supply to finish growing our tomato.",
    ]
    assert " ".join(parts) == text
    existing = runpy.run_path("C:/Users/Trole/Documents/generate_voice/generate_voice.py", run_name="gronny_existing_generator")
    assert existing["MODEL_REVISION"] == "v2"
    model_path = snapshot_download(
        repo_id=existing["MODEL_REPO_ID"], revision="v2",
        allow_patterns=existing["MODEL_FILES"], local_files_only=True,
    )
    print("Loading installed KittenTTS v2, Kiki; expressive default temperature 0.9.", flush=True)
    model = KittenTTS(model_path, weights="packed")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    pieces = []
    records = []
    for i, part in enumerate(parts):
        print(f"Generating natural take {i+1}/{len(parts)}...", flush=True)
        audio = np.asarray(model.generate(
            "[tender] " + part, voice="Kiki", preset="expressive", temperature=0.9,
            advanced={"seed": 281 + i},
        ), dtype=np.float32)
        assert audio.size and np.isfinite(audio).all()
        # Keep the generated pitch and delivery; fade only the boundary to avoid clicks.
        fade = min(round(0.005 * model.sample_rate), len(audio) // 2)
        audio[:fade] *= np.linspace(0, 1, fade, dtype=np.float32)
        audio[-fade:] *= np.linspace(1, 0, fade, dtype=np.float32)
        sf.write(OUTPUT / f"part_{i+1}.wav", audio, model.sample_rate)
        if pieces:
            pieces.append(np.zeros(round(0.4 * model.sample_rate), dtype=np.float32))
        pieces.append(audio)
        records.append({"part": i+1, "text": part, "seed": 281+i, "seconds": len(audio)/model.sample_rate})
    audio = np.concatenate(pieces)
    peak = float(np.max(np.abs(audio)))
    # Apply a single gain adjustment only if required to leave headroom.
    if peak > 0.98:
        audio *= 0.98 / peak
    path = OUTPUT / "01_welcome.wav"
    sf.write(path, audio, model.sample_rate)
    result = {
        "text": text, "voice": "Kiki", "model_revision": "v2", "preset": "expressive",
        "temperature": 0.9, "expression": "tender", "gap_seconds": 0.4,
        "pitch_and_speed_processing": False, "parts": records,
        "duration_seconds": len(audio)/model.sample_rate,
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest().upper(),
    }
    (OUTPUT / "Generation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2), flush=True)

if __name__ == "__main__":
    main()
