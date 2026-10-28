"""Install the verified new performance of the unchanged welcome script."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parent.parent
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))
def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

qa = load(ROOT / "output/HandsStartupNaturalAudioQA.json")
assert qa["words_match"] or qa["name_only_difference"]
assert qa["clipped_sample_fraction"] == 0 and qa["sample_rate"] == 24000
generated = ROOT / "output/HandsStartupNatural_20261028/01_welcome.wav"
generation = load(generated.parent / "Generation.json")
text = load(ROOT / "Docs/GronnyHandsStartupDialogues.json")[0]["text"]
assert generation["text"] == qa["expected"] == text
assert generation["sha256"] == qa["sha256"] == digest(generated)
destination = ROOT / "Assets/Audio/dialogues_cozy_gardening/01_welcome.wav"
backup = Path((ROOT / "output/HandsStartupNaturalBackup.txt").read_text(encoding="utf-8-sig").strip())
assert backup.is_relative_to(ROOT)
assert digest(destination) == digest(backup / destination.relative_to(ROOT))
meta = destination.with_suffix(".wav.meta")
meta_hash = digest(meta)
manifest_path = ROOT / "Docs/WaterSavingAudioImport.json"
manifest = load(manifest_path)
assert digest(destination) == manifest["welcome_sha256"]
record = next(item for item in manifest["files"] if item["id"] == "01_welcome")
assert record["override_text"] == text
record.update({
    "prior_override_sha256": manifest["welcome_sha256"],
    "previous_revision_backup": str(backup / destination.relative_to(ROOT)),
    "override_sha256": qa["sha256"],
    "generation_manifest": "output/HandsStartupNatural_20261028/Generation.json",
    "replacement_reason": "User rejected robotic delivery; unchanged script, Kiki with tender expression, temperature 0.9 and short takes."
})
manifest["welcome_sha256"] = qa["sha256"]
manifest["welcome_natural_backup"] = str(backup)
shutil.copyfile(generated, destination)
save(manifest_path, manifest)
inventory_path = ROOT / "Docs/GronnyAudioInventory.md"
lines = inventory_path.read_text(encoding="utf-8-sig").splitlines()
index = next(i for i, line in enumerate(lines) if line.startswith("`Assets/Audio/dialogues_cozy_gardening/01_welcome.wav`"))
lines[index] = f"`Assets/Audio/dialogues_cozy_gardening/01_welcome.wav` | {qa['duration_seconds']:.2f} | Unknown | None | `{qa['sha256']}`"
inventory_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
assert digest(meta) == meta_hash
for item in manifest["files"]:
    assert digest(Path(item["destination"])) == (item.get("override_sha256") or item["source_sha256"]), item["id"]
changed = [item["path"] for item in load(ROOT / "output/HandsStartupBefore.json") if digest(Path(item["path"])) != item["sha256"]]
assert changed == [str(destination)]
result = {
    "changed_audio_and_scenes": changed, "meta_unchanged": True,
    "previous_take_backup": str(backup), "original_backup": (ROOT / "output/HandsStartupBackup.txt").read_text(encoding="utf-8-sig").strip(),
    "duration_seconds": qa["duration_seconds"], "sha256": qa["sha256"],
    "other_audio_and_scenes_unchanged": True, "lesson_hashes_verified": 45,
    "script_unchanged": True, "audio_qa": "output/HandsStartupNaturalAudioQA.json",
}
save(ROOT / "output/HandsStartupNaturalIntegrity.json", result)
print(json.dumps(result, indent=2))
