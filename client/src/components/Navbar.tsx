import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function Navbar() {
  const { token, displayName, role, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <nav className="navbar">
      <Link to="/" className="brand">SlotKeeper</Link>
      <div className="nav-links">
        {token ? (
          <>
            <Link to="/">Resources</Link>
            <Link to="/my-bookings">My Bookings</Link>
            {role === "Admin" && <Link to="/admin">Admin</Link>}
            <span className="user-pill">{displayName}</span>
            <button
              onClick={() => {
                logout();
                navigate("/login");
              }}
            >
              Sign out
            </button>
          </>
        ) : (
          <>
            <Link to="/login">Sign in</Link>
            <Link to="/register">Register</Link>
          </>
        )}
      </div>
    </nav>
  );
}
