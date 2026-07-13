import { RouterProvider, createHashRouter } from "react-router-dom";
import { AuthProvider } from "./auth/AuthContext";
import { buildRouteObjects } from "./routes";

const router = createHashRouter(buildRouteObjects());

export default function App() {
  return (
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  );
}
