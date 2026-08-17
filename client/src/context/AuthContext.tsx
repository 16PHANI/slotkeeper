import { createContext, useContext, useEffect, useMemo, useState, ReactNode } from "react";
import { AuthResponse, UserRole } from "../types";

interface AuthState {
  token: string | null;
  displayName: string | null;
  role: UserRole | null;
}

interface AuthContextValue extends AuthState {
  login: (response: AuthResponse) => void;
  logout: () => void;
}

const STORAGE_KEY = "slotkeeper.session";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return { token: null, displayName: null, role: null };
    }

    try {
      return JSON.parse(raw) as AuthState;
    } catch {
      return { token: null, displayName: null, role: null };
    }
  });

  useEffect(() => {
    if (state.token) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }, [state]);

  const value = useMemo<AuthContextValue>(
    () => ({
      ...state,
      login: (response: AuthResponse) =>
        setState({ token: response.token, displayName: response.displayName, role: response.role }),
      logout: () => setState({ token: null, displayName: null, role: null }),
    }),
    [state]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside an AuthProvider.");
  }

  return context;
}
