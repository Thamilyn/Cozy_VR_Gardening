"""Integrate the approved ripening line after local audio verification."""
import hashlib
import json
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parent.parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def read_exact(path):
    return path.read_bytes().decode("utf-8-sig")


def save(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


text = load(ROOT / "output/UnripeTomatoesDialogues.json")[0]["text"]
caption = "Our tomatoes are slowly ripening."
qa = load(ROOT / "output/UnripeTomatoesVerification.json")["files"][0]
generated = ROOT / "output/UnripeTomatoesAudio/11_green_fruit.wav"
assert qa["id"] == "11_green_fruit" and qa["expected"] == text
assert qa["words_match"] and qa["clipped_sample_fraction"] == 0
assert qa["sample_rate"] == 24000 and qa["peak"] > 0.01
assert qa["sha256"] == digest(generated)

backup = Path((ROOT / "output/UnripeTomatoesBackup.txt").read_text(encoding="utf-8-sig").strip())
assert backup.resolve().is_relative_to(ROOT)
for saved in backup.rglob("*"):
    if saved.is_file():
        assert digest(ROOT / saved.relative_to(backup)) == digest(saved), str(saved)

edits = {}


def replace(relative, old, new):
    path = ROOT / relative
    current = edits.get(path, read_exact(path))
    assert current.count(old) == 1, (relative, old)
    edits[path] = current.replace(old, new)


old_current = "There's our green tomato! This time, you decide when to stop watering. Use the target to guide you."
old_original = "Look, green tomatoes! This time, you're in charge. Check what the plant needs and take care of it."
replace("Docs/GronnyWaterSavingDialogues.json", old_current, text)
replace("Docs/GronnyDialogues.json", old_original, text)
replace("Docs/GuionVozGronny.md", old_original, text)
replace("Docs/GuionVozGronny.md", "Al aparecer los frutos verdes.", "Al aparecer los frutos inmaduros.")
replace("Docs/GronnyInstructionCaptions.json", '"instruction": "A green tomato!"', f'"instruction": "{caption}"')

asset_path = ROOT / "Assets/Resources/GronnyAudio/Tutorial.asset"
asset = read_exact(asset_path)
newline = "\r\n" if "\r\n" in asset else "\n"
replace("Assets/Resources/GronnyAudio/Tutorial.asset",
        "    transcript: There's our green tomato! This time, you decide when to stop watering." + newline +
        "      Use the target to guide you.",
        "    transcript: Our tomatoes are slowly ripening. This time, you decide when to stop" + newline +
        "      watering. Use the target to guide you.")
replace("Assets/Resources/GronnyAudio/Tutorial.asset", "    instruction: A green tomato!", "    instruction: " + caption)

replace("Docs/GronnyAudioSystem.md", "`11_green_fruit` | Fase 4 | Ambas | Frutos verdes existentes",
        "`11_green_fruit` | Fase 4 | Ambas | Frutos inmaduros existentes, de distintos colores")
accessibility_path = ROOT / "Docs/GuionVozMenuYAccesibilidad.md"
accessibility = read_exact(accessibility_path)
nl = "\r\n" if "\r\n" in accessibility else "\n"
replace("Docs/GuionVozMenuYAccesibilidad.md",
        nl.join(["La mención descriptiva a green tomatoes en `11_green_fruit` se puede conservar:",
                 "no es la única señal para identificar un objeto ni ordena elegirlo por su color.",
                 "La fase y el cuidado seguirán mostrándose mediante texto e iconos."]),
        nl.join(["La grabación `11_green_fruit` se revisó el 29 de octubre de 2026 para decir",
                 "«Our tomatoes are slowly ripening.»: los frutos inmaduros del modelo tienen",
                 "distintos colores. El texto del panel coincide con la nueva frase; se conserva",
                 "el identificador del recurso. La fase y el cuidado se muestran mediante texto e iconos."]))

inventory = ROOT / "Docs/GronnyAudioInventory.md"
old_row = next(line for line in read_exact(inventory).splitlines()
               if line.startswith("`Assets/Audio/dialogues_cozy_gardening/11_green_fruit.wav`"))
replace("Docs/GronnyAudioInventory.md", old_row,
        f"`Assets/Audio/dialogues_cozy_gardening/11_green_fruit.wav` | {qa['duration_seconds']:.2f} | Unknown | None | `{qa['sha256']}`")

manifest_path = ROOT / "Docs/WaterSavingAudioImport.json"
manifest_text = read_exact(manifest_path)
match = re.search(r'^    \{\r?\n      "id": "11_green_fruit",[\s\S]*?^    \}', manifest_text, re.MULTILINE)
assert match
record = json.loads(match.group())
destination = ROOT / "Assets/Audio/dialogues_cozy_gardening/11_green_fruit.wav"
assert digest(destination) == (record.get("override_sha256") or record["source_sha256"])
record.update({
    "prior_override_sha256": digest(destination),
    "previous_revision_backup": str(backup / destination.relative_to(ROOT)),
    "override_sha256": qa["sha256"],
    "override_text": text,
    "replacement_reason": "Approved ripening description: the immature tomatoes have several colors.",
    "verification_report": "output/UnripeTomatoesVerification.json",
})
nl = "\r\n" if "\r\n" in manifest_text else "\n"
new_record = nl.join("    " + line for line in json.dumps(record, ensure_ascii=False, indent=2).splitlines())
replace("Docs/WaterSavingAudioImport.json", match.group(), new_record)

# Validate every replacement before touching the live clip or catalogue.
for path, content in edits.items():
    if path.suffix == ".json":
        json.loads(content)
shutil.copyfile(generated, destination)
for path, content in edits.items():
    path.write_bytes(content.encode("utf-8"))

assert digest(destination.with_suffix(".wav.meta")) == digest(backup / destination.with_suffix(".wav.meta").relative_to(ROOT))
manifest = load(manifest_path)
for item in manifest["files"]:
    path = Path(item["destination"])
    assert path.resolve().is_relative_to(ROOT)
    assert digest(path) == (item.get("override_sha256") or item["source_sha256"]), item["id"]
changed = [item["Path"] for item in load(ROOT / "output/UnripeTomatoesBefore.json")
           if digest(Path(item["Path"])) != item["SHA256"]]
assert set(changed) == {str(destination)} | {str(path) for path in edits if path != manifest_path}
result = {
    "text": text, "instruction": caption, "duration_seconds": qa["duration_seconds"],
    "voice": "Kiki", "model_revision": "v2", "preset": "expressive", "temperature": 1.1,
    "sha256": qa["sha256"], "backup": str(backup), "changed_baseline_files": changed,
    "changed_manifest": str(manifest_path), "other_audio_and_all_audio_meta_unchanged": True,
    "lesson_hashes_verified": len(manifest["files"]), "unity_play_and_quest_validation": "Not run",
}
save(ROOT / "output/UnripeTomatoesIntegrity.json", result)
print(json.dumps(result, ensure_ascii=False, indent=2))
