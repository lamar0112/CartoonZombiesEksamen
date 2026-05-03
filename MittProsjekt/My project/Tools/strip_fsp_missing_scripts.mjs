/**
 * Removes MonoBehaviour (114) blocks referencing FSP script GUID that has no .cs in project.
 * Usage: node strip_fsp_missing_scripts.mjs
 */
import { readFileSync, writeFileSync, readdirSync, statSync } from "fs";
import { join, relative } from "path";

const MISSING = "1f6be16025ec88e4c88efb60e6a61e8a";
const PROJECT_ROOT = join(import.meta.dirname, "..");
const FSP_ROOT = join(PROJECT_ROOT, "Assets", "ThirdParty", "FSP");

const PREFIX = "--- !u!114 &";

function walk(dir, acc = []) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (statSync(p).isDirectory()) walk(p, acc);
    else if (name.endsWith(".prefab") || name.endsWith(".unity")) acc.push(p);
  }
  return acc;
}

function stripFile(path) {
  const original = readFileSync(path, "utf8");
  let text = original;
  const removed = new Set();

  let out = "";
  let cursor = 0;
  while (cursor < text.length) {
    const hit = text.indexOf(PREFIX, cursor);
    if (hit === -1) {
      out += text.slice(cursor);
      break;
    }
    out += text.slice(cursor, hit);
    const idStart = hit + PREFIX.length;
    const idEnd = text.indexOf("\n", idStart);
    if (idEnd === -1) {
      out += text.slice(hit);
      break;
    }
    const id = text.slice(idStart, idEnd);
    const monoIdx = text.indexOf("MonoBehaviour:", idEnd);
    if (monoIdx === -1) {
      out += text.slice(hit, idEnd + 1);
      cursor = idEnd + 1;
      continue;
    }
    const nextDoc = text.indexOf("\n--- !u!", monoIdx);
    const blockEnd = nextDoc === -1 ? text.length : nextDoc + 1;
    const block = text.slice(hit, blockEnd);
    if (block.includes(`guid: ${MISSING}`)) {
      removed.add(id);
      cursor = blockEnd;
    } else {
      out += block;
      cursor = blockEnd;
    }
  }

  let final = out;
  for (const id of removed) {
    final = final.replace(
      new RegExp(`^[ \\t]*- component: \\{fileID: ${id}\\}[ \\t]*\r?\n`, "gm"),
      "",
    );
  }

  if (final !== original) {
    writeFileSync(path, final, "utf8");
    return removed.size;
  }
  return 0;
}

let touched = 0;
let blocks = 0;
for (const f of walk(FSP_ROOT)) {
  const n = stripFile(f);
  if (n) {
    touched++;
    blocks += n;
    console.log(`${relative(PROJECT_ROOT, f)}: removed ${n}`);
  }
}
console.log(`Done. Files changed: ${touched}, MonoBehaviour blocks removed: ${blocks}`);
