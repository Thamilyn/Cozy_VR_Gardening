import json
from pathlib import Path
from pypdf import PdfReader
import openpyxl

base = Path(__file__).parent
pdf = PdfReader(r'C:\Users\Trole\Documents\Conceptual design and requirements specification.pdf')
text = '\n\n'.join(f'PAGE {i+1}\n{p.extract_text()}' for i,p in enumerate(pdf.pages))
(base/'design.txt').write_text(text, encoding='utf-8')
print(text)
for i,name in enumerate(['Cozy VR Garden First Prototype Feedback(1-5).xlsx', "Cozy VR Garden_ Walking & Gronny's Guidance(1-1).xlsx"]):
    wb=openpyxl.load_workbook(Path(r'C:\Users\Trole\Downloads')/name, data_only=True)
    result={s.title:list(s.values) for s in wb}
    out=json.dumps(result,ensure_ascii=False,default=str,indent=2)
    (base/f'testing-{i+1}.json').write_text(out,encoding='utf-8')
    print(name+'\n'+out)
