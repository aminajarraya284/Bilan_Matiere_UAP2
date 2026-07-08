import json
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

NS = {
    "m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "rel": "http://schemas.openxmlformats.org/package/2006/relationships",
}


def col_to_idx(ref: str) -> int:
    letters = "".join(ch for ch in ref if ch.isalpha())
    idx = 0
    for ch in letters:
        idx = idx * 26 + ord(ch.upper()) - 64
    return idx - 1


def read_shared_strings(zf: zipfile.ZipFile) -> list[str]:
    if "xl/sharedStrings.xml" not in zf.namelist():
      return []

    root = ET.fromstring(zf.read("xl/sharedStrings.xml"))
    return ["".join(t.text or "" for t in si.findall(".//m:t", NS)) for si in root.findall("m:si", NS)]


def cell_value(cell: ET.Element, shared_strings: list[str]) -> str:
    cell_type = cell.attrib.get("t")
    value = cell.find("m:v", NS)
    inline = cell.find("m:is/m:t", NS)

    if cell_type == "s" and value is not None and value.text:
        index = int(value.text)
        return shared_strings[index] if index < len(shared_strings) else ""
    if inline is not None:
        return inline.text or ""
    if value is not None:
        return value.text or ""
    return ""


def read_rows(zf: zipfile.ZipFile, sheet_file: str):
    shared_strings = read_shared_strings(zf)
    root = ET.fromstring(zf.read(sheet_file))

    for row in root.findall("m:sheetData/m:row", NS):
        values = []
        last_idx = -1
        for cell in row.findall("m:c", NS):
            idx = col_to_idx(cell.attrib.get("r", "A1"))
            while last_idx + 1 < idx:
                values.append("")
                last_idx += 1
            values.append(cell_value(cell, shared_strings))
            last_idx = idx
        yield values


def as_number(value) -> float:
    try:
        if value in ("", None):
            return 0
        return float(str(value).replace(",", "."))
    except ValueError:
        return 0


def clean(value) -> str:
    return str(value or "").strip()


def looks_like_material(code: str) -> bool:
    return bool(re.match(r"^M[A-Z0-9]+", code))


def main() -> None:
    source = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(r"C:\Users\user\Downloads\ListeOFX3.xlsx")
    public_dir = Path("public/reference")
    src_data_dir = Path("src/data")
    public_dir.mkdir(parents=True, exist_ok=True)
    src_data_dir.mkdir(parents=True, exist_ok=True)

    with zipfile.ZipFile(source) as zf:
        list_rows = list(read_rows(zf, "xl/worksheets/sheet1.xml"))
        gamme_rows = list(read_rows(zf, "xl/worksheets/sheet2.xml"))
        bom_rows = list(read_rows(zf, "xl/worksheets/sheet3.xml"))

    sorties_map = defaultdict(lambda: {
        "qtePrevue": 0,
        "qteRealisee": 0,
        "qteRestante": 0,
        "qteBesoin": 0,
        "qteConsommee": 0,
    })
    of_set = set()
    component_set = set()

    for row in list_rows[2:]:
        of = clean(row[0] if len(row) > 0 else "")
        component = clean(row[1] if len(row) > 1 else "")
        if not of or not component:
            continue

        key = (of, component)
        item = sorties_map[key]
        item["of"] = of
        item["composant"] = component
        item["qtePrevue"] += as_number(row[2] if len(row) > 2 else 0)
        item["qteRealisee"] += as_number(row[3] if len(row) > 3 else 0)
        item["qteRestante"] += as_number(row[4] if len(row) > 4 else 0)
        item["qteBesoin"] += as_number(row[5] if len(row) > 5 else 0)
        item["qteConsommee"] += as_number(row[6] if len(row) > 6 else 0)
        of_set.add(of)
        component_set.add(component)

    articles = {}
    postes = set()
    ateliers = set()
    for row in gamme_rows[1:]:
        code = clean(row[0] if len(row) > 0 else "")
        designation = clean(row[1] if len(row) > 1 else "")
        atelier = clean(row[4] if len(row) > 4 else "")
        poste = clean(row[5] if len(row) > 5 else "")
        if not code or not designation:
            continue
        articles.setdefault(code, {"code": code, "designation": designation, "atelier": atelier, "poste": poste})
        if atelier:
            ateliers.add(atelier)
        if poste:
            postes.add(poste)

    materials = {}
    for row in bom_rows[1:]:
        code = clean(row[0] if len(row) > 0 else "")
        designation = clean(row[1] if len(row) > 1 else "")
        if code and designation and looks_like_material(code):
            materials.setdefault(code, {"code": code, "designation": designation})

    for component in component_set:
        if looks_like_material(component):
            materials.setdefault(component, {"code": component, "designation": component})

    sorties = sorted(sorties_map.values(), key=lambda item: (item["of"], item["composant"]))
    article_list = sorted(articles.values(), key=lambda item: item["code"])
    material_list = sorted(materials.values(), key=lambda item: item["code"])

    reference = {
        "sourceFile": source.name,
        "generatedFrom": str(source),
        "sheets": {
            "LISTEOFX3": {"rawRows": len(list_rows), "aggregatedRows": len(sorties), "uniqueOf": len(of_set), "uniqueComponents": len(component_set)},
            "GAMME": {"rawRows": len(gamme_rows), "uniqueArticles": len(article_list), "uniqueAteliers": len(ateliers), "uniquePostes": len(postes)},
            "BOM": {"rawRows": len(bom_rows), "uniqueMaterials": len(material_list)},
        },
        "sortiesX3": sorties,
        "articles": article_list,
        "materials": material_list,
        "ateliers": sorted(ateliers),
        "postes": sorted(postes),
    }

    json_path = public_dir / "x3-reference.json"
    json_path.write_text(json.dumps(reference, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")

    ts_path = src_data_dir / "x3Reference.ts"
    ts_path.write_text(
        "export const X3_REFERENCE_URL = '/reference/x3-reference.json';\n"
        "export const X3_REFERENCE_WORKBOOK_URL = '/reference/ListeOFX3.xlsx';\n"
        f"export const X3_REFERENCE_COUNTS = {json.dumps(reference['sheets'], ensure_ascii=False, indent=2)} as const;\n",
        encoding="utf-8",
    )

    print(json.dumps({
        "json": str(json_path),
        "typescript": str(ts_path),
        "counts": reference["sheets"],
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
