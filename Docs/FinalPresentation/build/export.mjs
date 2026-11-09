import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
import {Presentation,PresentationFile} from 'file:///C:/Users/Trole/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const root='C:/Users/Trole/Cozy_VR_Gardening/Docs/FinalPresentation';
const skill='C:/Users/Trole/.codex/plugins/cache/openai-primary-runtime/presentations/26.1105.12137/skills/presentations';
const presentation=Presentation.load(JSON.parse(await fs.readFile(root+'/build/final-state.json','utf8')));
const imagePaths={0:'C:/Users/Trole/Cozy_VR_Gardening/Docs/Validation/Pastelday.png',1:'C:/Users/Trole/Downloads/photo_2026-11-08_23-54-38.jpg',2:'C:/Users/Trole/Cozy_VR_Gardening/Docs/Validation/ShovelSoilPlacement.png',3:'C:/Users/Trole/Downloads/photo_2026-11-08_23-54-48.jpg',8:'C:/Users/Trole/Cozy_VR_Gardening/Docs/Validation/PastelMatureTomatoReview.png'};
for(const [idx,p] of Object.entries(imagePaths)){const bytes=await fs.readFile(p);presentation.slides.items[Number(idx)].images.items[0].replace({blob:new Uint8Array(bytes),contentType:p.endsWith('.jpg')?'image/jpeg':'image/png'});}
await fs.mkdir(root+'/output',{recursive:true});
const candidate=root+'/build/candidate.pptx';
await (await PresentationFile.exportPptx(presentation)).save(candidate);
const {finalizePresentation}=await import(pathToFileURL(path.join(skill,'container_tools/artifact_tool_utils.mjs')).href);
const result=await finalizePresentation({workspaceDir:root,candidatePath:candidate,finalPath:root+'/output/Cozy_VR_Garden_v2.pptx',pythonExecutable:'C:/Users/Trole/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',integrityValidatorPath:skill+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:skill+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],fontPolicy:{basis:'design',families:['Arial']},materializeLiteralChartWorkbooks:true,requiredNativeChartOwnerSlides:[5],requiredNativeTableOwnerSlides:[],verifyArtifactToolImport:true,receiptPath:root+'/build/validation-v2.json'});
console.log(JSON.stringify(result));
