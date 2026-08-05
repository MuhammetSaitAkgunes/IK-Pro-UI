import { RouterProvider, createHashRouter } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "./auth/AuthContext";
import { ToastProvider } from "./layout/ToastProvider";
import { createQueryClient } from "./queryClient";
import { buildRouteObjects } from "./routes";

const router = createHashRouter(buildRouteObjects());
const queryClient = createQueryClient();

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ToastProvider>
          <RouterProvider router={router} />
        </ToastProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}
