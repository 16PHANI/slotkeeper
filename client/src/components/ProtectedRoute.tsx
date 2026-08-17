import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function ProtectedRoute({ requireAdmin = false }: { requireAdmin?: boolean }) {
  const { token, role } = useAuth();

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (requireAdmin && role !== "Admin") {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
