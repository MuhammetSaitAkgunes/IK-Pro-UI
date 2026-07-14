export const tableToCsvLines = (
  table: HTMLTableElement,
  rowFilter?: (row: HTMLTableRowElement) => boolean,
): string[] => {
  const lines: string[] = [];
  table.querySelectorAll("tr").forEach((row) => {
    if (row.style.display === "none") return;
    if (rowFilter && !rowFilter(row)) return;

    const cells = Array.from(row.querySelectorAll("th, td"))
      .filter((cell) => !cell.classList.contains("csv-skip"))
      .map((cell) => {
        // jsdom innerText desteklemez; textContent aynı hücre metnini verir.
        const text = (cell as HTMLElement).innerText ?? cell.textContent ?? "";
        return `"${text.replace(/\s+/g, " ").trim().replace(/"/g, '""')}"`;
      });
    if (cells.length) lines.push(cells.join(";"));
  });
  return lines;
};

export const downloadCsv = (lines: string[], fileName: string): void => {
  // BOM: Excel'in Türkçe karakterleri UTF-8 olarak açması için.
  const blob = new Blob(["﻿" + lines.join("\n")], { type: "text/csv;charset=utf-8;" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `${fileName}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(link.href);
};
