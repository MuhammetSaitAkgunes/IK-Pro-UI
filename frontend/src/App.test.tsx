import { render, screen } from "@testing-library/react";
import App from "./App";

test("uygulama başlığı render edilir", () => {
  render(<App />);
  expect(screen.getByText("İK Pro")).toBeInTheDocument();
});
