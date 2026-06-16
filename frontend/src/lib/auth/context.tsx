"use client";

import { createContext, useContext, useEffect, useState } from "react";
import type { Identidad } from "./types";

const STORAGE_KEY = "esports-identidad";

interface AuthCtx {
  identidad: Identidad | null;
  setIdentidad: (i: Identidad | null) => void;
  logout: () => void;
}

const Ctx = createContext<AuthCtx>({
  identidad: null,
  setIdentidad: () => {},
  logout: () => {},
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [identidad, setIdentidadState] = useState<Identidad | null>(null);

  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) setIdentidadState(JSON.parse(stored));
    } catch {}
  }, []);

  const setIdentidad = (i: Identidad | null) => {
    setIdentidadState(i);
    if (i) localStorage.setItem(STORAGE_KEY, JSON.stringify(i));
    else localStorage.removeItem(STORAGE_KEY);
  };

  const logout = () => setIdentidad(null);

  return <Ctx.Provider value={{ identidad, setIdentidad, logout }}>{children}</Ctx.Provider>;
}

export function useAuth() {
  return useContext(Ctx);
}
