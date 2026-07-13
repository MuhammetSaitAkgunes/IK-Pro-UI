import { render, screen } from "@testing-library/react";
import App from "./App";

test("oturum yokken giriş ekranı açılır", () => {
  localStorage.clear();
  render(<App />);
  expect(screen.getAllByText("Giriş yap").length).toBeGreaterThan(0);
});
