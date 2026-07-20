/*
Simple XLS/XLSX inspection + OF extraction script.
Usage:
  1) npm install xlsx
  2) node scripts/extract_ofs.js "YListOFFerm (1).xls"

What it does:
- prints sheet names and first 20 rows of each sheet to the console
- attempts to extract a column that looks like an OF (strings matching /OF-?\d+/i)
- writes extracted OFs to data/of_list.json and data/of_list.txt
*/

const fs = require('fs');
const path = require('path');
let XLSX;
try {
  XLSX = require('xlsx');
} catch (e) {
  console.error('Module "xlsx" not found. Install with: npm install xlsx');
  process.exit(1);
}

const argv = process.argv.slice(2);
if (!argv[0]) {
  console.error('Please provide the Excel file path as argument. Example: node scripts/extract_ofs.js "YListOFFerm (1).xls"');
  process.exit(1);
}

const filePath = path.resolve(argv[0]);
if (!fs.existsSync(filePath)) {
  console.error('File not found:', filePath);
  process.exit(1);
}

const workbook = XLSX.readFile(filePath, { raw: true });
const sheets = workbook.SheetNames;
console.log('Sheets found:', sheets);

const ofCandidates = new Set();

sheets.forEach(sheetName => {
  const sheet = workbook.Sheets[sheetName];
  const json = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '' });
  console.log(`\n--- Sheet: ${sheetName} (showing up to 20 rows) ---`);
  json.slice(0, 20).forEach((row, idx) => console.log(idx + 1, row));

  // scan rows for OF-like strings
  json.forEach(row => {
    row.forEach(cell => {
      if (typeof cell === 'string' && /OF-?\d{3,}|^\d{4,}$/i.test(cell.trim())) {
        // normalize
        const v = cell.trim().toUpperCase().replace(/\s+/g, '');
        ofCandidates.add(v);
      }
      // also numbers that could be OF numeric codes
      if (typeof cell === 'number') {
        const v = String(cell);
        if (/^\d{4,}$/.test(v)) ofCandidates.add('OF-' + v);
      }
    });
  });
});

const ofs = Array.from(ofCandidates).sort();
if (!fs.existsSync(path.join(__dirname, '..', 'data'))) fs.mkdirSync(path.join(__dirname, '..', 'data'));
fs.writeFileSync(path.join(__dirname, '..', 'data', 'of_list.json'), JSON.stringify(ofs, null, 2), 'utf8');
fs.writeFileSync(path.join(__dirname, '..', 'data', 'of_list.txt'), ofs.join('\n'), 'utf8');

console.log('\nExtracted OF candidates:', ofs.length);
console.log('Saved to data/of_list.json and data/of_list.txt');

// Print a short preview
console.log('\nPreview (first 40 OFs):');
ofs.slice(0, 40).forEach((o, i) => console.log(i + 1, o));
