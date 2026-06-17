"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { login as apiLogin, me } from "@/lib/api/auth";
import { getToken, setToken, clearToken } from "./token";
import type { Identidad, Rol } from "./types";

interface AuthCtx {
  identidad: Identidad | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  /** @deprecated — solo para compatibilidad temporal mientras migran las páginas */
  setIdentidad: (i: Identidad | null) => void;
}

const Ctx = createContext<AuthCtx>({
  identidad: null,
  loading: true,
  login: async () => {},
  logout: () => {},
  setIdentidad: () => {},
});

function buildIdentidad(data: {
  username: string;
  rol: string;
  nombre: string;
  organizadorId?: string;
  equipoId?: string;
}): Identidad {
  const rol = data.rol as Rol;
  switch (rol) {
    case "admin":
      return { rol: "admin", username: data.username, nombre: data.nombre };
    case "organizador":
      return { rol: "organizador", username: data.username, organizadorId: data.organizadorId!, nombre: data.nombre };
    case "capitan":
      return { rol: "capitan", username: data.username, equipoId: data.equipoId!, nombre: data.nombre };
    default:
      return { rol: "fan", username: data.username, nombre: data.nombre };
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [identidad, setIdentidadState] = useState<Identidad | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  // Hidrata desde el JWT existente al montar
  useEffect(() => {
    const token = getToken();
    if (!token) { setLoading(false); return; }

    me()
      .then((data) => setIdentidadState(buildIdentidad(data)))
      .catch(() => { clearToken(); setIdentidadState(null); })
      .finally(() => setLoading(false));
  }, []);

  // Escucha 401 globales del fetcher
  useEffect(() => {
    const handler = () => {
      clearToken();
      setIdentidadState(null);
      router.push("/login");
    };
    window.addEventListener("esports:unauthorized", handler);
    return () => window.removeEventListener("esports:unauthorized", handler);
  }, [router]);

  const login = useCallback(async (username: string, password: string) => {
    const data = await apiLogin(username, password);
    setToken(data.token);
    const id = buildIdentidad(data);
    setIdentidadState(id);
    // Redirect según rol
    if (id.rol === "admin" || id.rol === "organizador") router.push("/panel");
    else if (id.rol === "capitan") router.push("/mi-equipo");
    else router.push("/");
  }, [router]);

  const logout = useCallback(() => {
    clearToken();
    setIdentidadState(null);
    router.push("/login");
  }, [router]);

  const setIdentidad = useCallback((i: Identidad | null) => {
    setIdentidadState(i);
    if (!i) clearToken();
  }, []);

  return (
    <Ctx.Provider value={{ identidad, loading, login, logout, setIdentidad }}>
      {children}
    </Ctx.Provider>
  );
}

export function useAuth() {
  return useContext(Ctx);
}
