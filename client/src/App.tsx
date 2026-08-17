import { Route, Routes } from "react-router-dom";
import { Navbar } from "./components/Navbar";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { ResourcesPage } from "./pages/ResourcesPage";
import { MyBookingsPage } from "./pages/MyBookingsPage";
import { AdminResourcesPage } from "./pages/AdminResourcesPage";

export default function App() {
  return (
    <div className="app-shell">
      <Navbar />
      <main className="content">
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<ResourcesPage />} />
            <Route path="/my-bookings" element={<MyBookingsPage />} />
          </Route>
          <Route element={<ProtectedRoute requireAdmin />}>
            <Route path="/admin" element={<AdminResourcesPage />} />
          </Route>
        </Routes>
      </main>
    </div>
  );
}
