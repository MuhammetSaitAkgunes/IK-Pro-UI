import { expect, test } from "vitest";
import {
  actionLevelText, actionPillClass, actionStatusText, nextActionStatus, nextActionStatusLabel,
} from "./format";

test("öncelik/durum etiketleri eski eşlemelerle birebir", () => {
  expect(actionLevelText("high")).toBe("Yüksek");
  expect(actionLevelText("medium")).toBe("Orta");
  expect(actionLevelText("low")).toBe("Düşük");
  expect(actionLevelText("bilinmeyen")).toBe("Normal");
  expect(actionStatusText("open")).toBe("Açık");
  expect(actionStatusText("week")).toBe("Bu Hafta");
  expect(actionStatusText("done")).toBe("Tamamlandı");
  expect(actionStatusText(null)).toBe("Açık");
});

test("öncelik pill sınıfı eski üçlü eşleme", () => {
  expect(actionPillClass("high")).toBe("rejected");
  expect(actionPillClass("medium")).toBe("pending");
  expect(actionPillClass("low")).toBe("approved");
});

test("ileri yönlü durum geçişi", () => {
  expect(nextActionStatus("open")).toBe("week");
  expect(nextActionStatus("week")).toBe("done");
  expect(nextActionStatus("done")).toBeNull();
  expect(nextActionStatusLabel("open")).toBe("Bu haftaya al");
  expect(nextActionStatusLabel("week")).toBe("Tamamlandı işaretle");
  expect(nextActionStatusLabel("done")).toBeNull();
});
