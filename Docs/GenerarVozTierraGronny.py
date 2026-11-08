"""Generate only the three soil and shovel cues with Gronny's installed offline voice."""
import hashlib
import json
import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "output/GronnySoilVoice_20261108"
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"
os.environ["PYTHONDONTWRITEBYTECODE"] = "1"
sys.dont_write_bytecode = True


def main():
    entries = json.loads((ROOT / "Docs/GronnySoilDialogues.json").read_text(encoding="utf-8-sig"))
    if len(sys.argv) > 1:
        entries = [entry for entry in entries if entry["id"] in sys.argv[1:]]
        if not entries:
            raise SystemExit("No matching soil cue requested.")
    for entry in entries:
        if (OUTPUT / (entry["id"] + ".wav")).exists():
            raise SystemExit("Preserve the existing take before generating again: " + entry["id"])
    OUTPUT.mkdir(parents=True, exist_ok=True)
    temporary = OUTPUT / "temp"
    temporary.mkdir(exist_ok=True)
    for variable in ("TMP", "TEMP", "TMPDIR"):
        os.environ[variable] = str(temporary)
    os.environ["TORCH_EXTENSIONS_DIR"] = str(OUTPUT / "torch_extensions")

    import numpy as np
    import soundfile as sf
    from huggingface_hub import snapshot_download
    from kittenml import KittenTTS

    model_path = snapshot_download(
        repo_id="KittenML/kitten-tts-2", revision="v2", local_files_only=True,
        allow_patterns=["config.json", "lm/*.json", "lm/*.txt", "lm/model-ternary.safetensors", "speaker/*", "voices/*"],
    )
    print("Loading installed KittenTTS v2, Kiki, expressive/tender, offline.", flush=True)
    model = KittenTTS(model_path, weights="packed")
    previous_manifest = OUTPUT / "Generation.json"
    records = json.loads(previous_manifest.read_text(encoding="utf-8"))["recordings"] if previous_manifest.exists() else []
    for entry in entries:
        print("Generating " + entry["id"], flush=True)
        audio = np.asarray(model.generate(
            "[tender] " + entry["text"], voice="Kiki", preset="expressive", temperature=0.9,
            advanced={"seed": entry["seed"]},
        ), dtype=np.float32)
        if audio.size == 0 or not np.isfinite(audio).all():
            raise RuntimeError("Invalid generated signal: " + entry["id"])
        fade = min(round(0.005 * model.sample_rate), len(audio) // 2)
        if fade:
            audio[:fade] *= np.linspace(0, 1, fade, dtype=np.float32)
            audio[-fade:] *= np.linspace(1, 0, fade, dtype=np.float32)
        peak = float(np.max(np.abs(audio)))
        if peak > 0.98:
            audio *= 0.98 / peak
        path = OUTPUT / (entry["id"] + ".wav")
        sf.write(path, audio, model.sample_rate, subtype="PCM_16")
        records = [record for record in records if record["id"] != entry["id"]]
        records.append(dict(entry, sample_rate=model.sample_rate, seconds=len(audio) / model.sample_rate,
            peak=float(np.max(np.abs(audio))), sha256=hashlib.sha256(path.read_bytes()).hexdigest().upper()))
        print(json.dumps(records[-1]), flush=True)
    manifest = dict(model="KittenML/kitten-tts-2", revision="v2", voice="Kiki", preset="expressive",
        expression="tender", temperature=0.9, pitch_and_speed_processing=False, recordings=records)
    (OUTPUT / "Generation.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
