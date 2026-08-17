import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, ApiError } from "../api/client";
import { AuthResponse } from "../types";
import { useAuth } from "../context/AuthContext";

export function RegisterPage() {
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const response = await api.post<AuthResponse>("/api/auth/register", { email, password, displayName });
      login(response);
      navigate("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong. Try again.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit}>
      <h1>Create an account</h1>
      {error && <p className="form-error">{error}</p>}
      <label>
        Name
        <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
      </label>
      <label>
        Email
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
      </label>
      <label>
        Password
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
      </label>
      <button type="submit" disabled={busy}>
        {busy ? "Creating account..." : "Create account"}
      </button>
    </form>
  );
}
