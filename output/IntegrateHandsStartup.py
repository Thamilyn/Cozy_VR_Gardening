"""Replace only the approved welcome, preserving Unity references and other audio."""
import hashlib
import json
from pathlib import Path
import shutil
import textwrap

ROOT = Path(__file__).resolve().parent.parent
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def read_json(relative):
    return json.loads((ROOT / relative).read_text(encoding="utf-8-sig"))
def write_json(relative, data):
    (ROOT / relative).write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

qa = read_json("output/HandsStartupAudioQA.json")
assert qa["words_match"] or qa["name_only_difference"]
assert qa["clipped_sample_fraction"] == 0 and qa["sample_rate"] == 24000
line = read_json("Docs/GronnyHandsStartupDialogues.json")[0]
new_text = line["text"]
assert "sustainable gardening" in new_text and "limited water supply" in new_text
assert "hold both hands comfortably in front of you" in new_text
assert "gardening table" not in new_text

backup = Path((ROOT / "output/HandsStartupBackup.txt").read_text(encoding="utf-8-sig").strip())
assert backup.is_relative_to(ROOT) and backup.is_dir()
manifest = read_json("Docs/WaterSavingAudioImport.json")
old_hash = manifest["welcome_sha256"]
destination = ROOT / "Assets/Audio/dialogues_cozy_gardening/01_welcome.wav"
source = ROOT / "output/HandsStartupAudio_20261028/01_welcome.wav"
assert digest(source) == qa["sha256"]
assert digest(destination) == old_hash
assert digest(backup / destination.relative_to(ROOT)) == old_hash
meta = destination.with_suffix(".wav.meta")
meta_hash = digest(meta)

old_text = read_json("Docs/GronnyWelcomeDialogues.json")[0]["text"]
json_updates = {}
for relative in ("Docs/GronnyWelcomeDialogues.json", "Docs/GronnyWelcomeAndMovementDialogues.json", "Docs/GronnyWaterSavingDialogues.json"):
    content = (ROOT / relative).read_text(encoding="utf-8-sig")
    entries = json.loads(content)
    assert next(item for item in entries if item["id"] == "01_welcome")["text"] == old_text
    before = json.dumps(old_text, ensure_ascii=False)
    assert content.count(before) == 1
    json_updates[relative] = content.replace(before, json.dumps(new_text, ensure_ascii=False))

catalogue_path = ROOT / "Assets/Resources/GronnyAudio/Tutorial.asset"
catalogue = catalogue_path.read_text(encoding="utf-8-sig")
start = catalogue.index("    transcript:", catalogue.index("  - id: 01_welcome\n"))
end = catalogue.index("    steps:", start)
assert " ".join(catalogue[start:end].strip().removeprefix("transcript:").split()) == old_text
new_transcript = textwrap.fill(new_text, width=80, initial_indent="    transcript: ", subsequent_indent="      ") + "\n"
catalogue = catalogue[:start] + new_transcript + catalogue[end:]

shutil.copyfile(source, destination)
for relative, content in json_updates.items():
    (ROOT / relative).write_text(content, encoding="utf-8")
catalogue_path.write_text(catalogue, encoding="utf-8")
record = next(item for item in manifest["files"] if item["id"] == "01_welcome")
record.update({
    "previous_revision_backup": str(backup / destination.relative_to(ROOT)),
    "prior_override_sha256": old_hash, "override_sha256": qa["sha256"],
    "override_text": new_text, "override_source": "Docs/GronnyHandsStartupDialogues.json",
    "replacement_reason": "Approved startup hand instruction; sustainable gardening retained; table invitation only after movement."
})
manifest["welcome_sha256"] = qa["sha256"]
manifest["welcome_unchanged"] = False
manifest["welcome_hands_startup_backup"] = str(backup)
write_json("Docs/WaterSavingAudioImport.json", manifest)

inventory_path = ROOT / "Docs/GronnyAudioInventory.md"
inventory = inventory_path.read_text(encoding="utf-8-sig")
lines = inventory.splitlines()
index = next(i for i, text in enumerate(lines) if text.startswith("`Assets/Audio/dialogues_cozy_gardening/01_welcome.wav`"))
lines[index] = f"`Assets/Audio/dialogues_cozy_gardening/01_welcome.wav` | {qa['duration_seconds']:.2f} | Unknown | None | `{qa['sha256']}`"
inventory_path.write_text("\n".join(lines) + "\n", encoding="utf-8")

before = read_json("output/HandsStartupBefore.json")
changed = [item["path"] for item in before if digest(Path(item["path"])) != item["sha256"]]
assert changed == [str(destination)], changed
assert digest(meta) == meta_hash
assert next(item for item in read_json("Docs/GronnyWelcomeAndMovementDialogues.json") if item["id"] == "movement_success")["text"] == "That's it! Now come over to the gardening table."
assert "guid: e4976e1c1644c9548bae1e00739b9e74" in meta.read_text()
result = {"changed_audio_and_scenes": changed, "meta_unchanged": True, "backup": str(backup), "duration_seconds": qa["duration_seconds"], "sha256": qa["sha256"], "other_audio_and_scenes_unchanged": True}
write_json("output/HandsStartupIntegrity.json", result)
print(json.dumps(result, indent=2))
