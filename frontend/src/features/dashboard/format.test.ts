import { expect, test } from "vitest";
import { getLevelText, getRiskLabel, getRiskLevel, priorityLabel, topPriorityActions } from "./format";

test("risk seviyesi eşikleri eski getRiskLevel ile birebir", () => {
  expect(getRiskLevel(70)).toBe("high");
  expect(getRiskLevel(55)).toBe("medium");
  expect(getRiskLevel(54)).toBe("low");
});

test("risk etiketi eski getRiskLabel ile birebir", () => {
  expect(getRiskLabel(70)).toBe("Yüksek risk");
  expect(getRiskLabel(55)).toBe("Orta risk");
  expect(getRiskLabel(40)).toBe("Kontrollü");
});

test("seviye metni bilinmeyen seviyede İzlemede döner", () => {
  expect(getLevelText("high")).toBe("Yüksek");
  expect(getLevelText(null)).toBe("İzlemede");
});

test("öncelik etiketi high/medium/low eşlemesi", () => {
  expect(priorityLabel("high")).toBe("Bugün müdahale et");
  expect(priorityLabel("medium")).toBe("Bu hafta takip et");
  expect(priorityLabel("low")).toBe("İzlemede kalsın");
});

test("topPriorityActions önceliğe göre sıralar ve keser", () => {
  const items = [{ priority: "low" }, { priority: "high" }, { priority: "medium" }, { priority: "high" }];
  expect(topPriorityActions(items, 3).map((i) => i.priority)).toEqual(["high", "high", "medium"]);
});
