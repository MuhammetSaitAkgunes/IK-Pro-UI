import { expect, test } from "vitest";
import { tableToCsvLines } from "./csv";

const buildTable = (html: string): HTMLTableElement => {
  const table = document.createElement("table");
  table.innerHTML = html;
  return table;
};

test("csv-skip hücreleri atlanır, değerler tırnaklanır", () => {
  const table = buildTable(`
    <thead><tr><th class="csv-skip">Seç</th><th>Ad</th><th>Departman</th></tr></thead>
    <tbody><tr><td class="csv-skip">x</td><td>Ahmet "Usta" Yılmaz</td><td>Yazılım</td></tr></tbody>
  `);
  expect(tableToCsvLines(table)).toEqual(['"Ad";"Departman"', '"Ahmet ""Usta"" Yılmaz";"Yazılım"']);
});

test("rowFilter false dönen satırları atlar", () => {
  const table = buildTable(`
    <tbody><tr data-keep="1"><td>Bir</td></tr><tr><td>İki</td></tr></tbody>
  `);
  const lines = tableToCsvLines(table, (row) => row.dataset.keep === "1");
  expect(lines).toEqual(['"Bir"']);
});
