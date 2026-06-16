"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Gamepad2, Trophy, Eye, Loader2, CheckCircle2 } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { getOrganizadores } from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { useAuth } from "@/lib/auth/context";
import type { Identidad } from "@/lib/auth/types";
import { toast } from "sonner";

type RolOpcion = "organizador" | "capitan" | "fan";

const roles: { id: RolOpcion; title: string; desc: string; icon: React.ElementType; color: string }[] = [
  {
    id: "organizador",
    title: "Organizador",
    desc: "Creás torneos, registrás videojuegos, asignás premios y registrás partidas.",
    icon: Trophy,
    color: "border-warning/40 hover:border-warning/70 data-[selected=true]:border-warning data-[selected=true]:bg-warning/5",
  },
  {
    id: "capitan",
    title: "Capitán",
    desc: "Gestionás tu equipo, agregás jugadores e inscribís el equipo en torneos.",
    icon: Gamepad2,
    color: "border-primary/40 hover:border-primary/70 data-[selected=true]:border-primary data-[selected=true]:bg-primary/5",
  },
  {
    id: "fan",
    title: "Fan / Visitante",
    desc: "Acceso de solo lectura. Podés explorar rankings, equipos, torneos y partidas.",
    icon: Eye,
    color: "border-border hover:border-muted-foreground data-[selected=true]:border-muted-foreground data-[selected=true]:bg-secondary",
  },
];

export default function LoginPage() {
  const [rolSeleccionado, setRolSeleccionado] = useState<RolOpcion | null>(null);
  const [orgId, setOrgId] = useState("");
  const [equipoId, setEquipoId] = useState("");
  const router = useRouter();
  const { setIdentidad } = useAuth();

  const { data: organizadores, isLoading: loadingOrgs } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
    enabled: rolSeleccionado === "organizador",
  });

  const { data: equipos, isLoading: loadingEquipos } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
    enabled: rolSeleccionado === "capitan",
  });

  const ingresar = () => {
    let identidad: Identidad;

    if (rolSeleccionado === "organizador") {
      const org = organizadores?.find((o) => o.organizadorId === orgId);
      if (!org) { toast.error("Seleccioná un organizador"); return; }
      identidad = { rol: "organizador", organizadorId: org.organizadorId, nombre: org.nombre };
    } else if (rolSeleccionado === "capitan") {
      const equipo = equipos?.find((e) => e.equipoId === equipoId);
      if (!equipo) { toast.error("Seleccioná un equipo"); return; }
      identidad = { rol: "capitan", equipoId: equipo.equipoId, nombre: equipo.nombre, tag: equipo.tag };
    } else {
      identidad = { rol: "fan" };
    }

    setIdentidad(identidad);
    toast.success("¡Bienvenido!");
    router.push("/");
  };

  return (
    <div className="max-w-xl mx-auto space-y-6 pt-8">
      <div className="text-center">
        <h1 className="text-2xl font-bold text-foreground">Elegí tu rol</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Tu rol determina qué acciones podés realizar en la plataforma.
        </p>
      </div>

      <div className="space-y-3">
        {roles.map(({ id, title, desc, icon: Icon, color }) => (
          <Card
            key={id}
            data-selected={rolSeleccionado === id}
            className={`cursor-pointer transition-colors ${color}`}
            onClick={() => { setRolSeleccionado(id); setOrgId(""); setEquipoId(""); }}
          >
            <CardHeader className="py-4">
              <div className="flex items-center gap-3">
                <Icon className="h-5 w-5 text-muted-foreground" />
                <div className="flex-1">
                  <CardTitle className="text-sm">{title}</CardTitle>
                  <CardDescription className="text-xs mt-0.5">{desc}</CardDescription>
                </div>
                {rolSeleccionado === id && <CheckCircle2 className="h-5 w-5 text-primary shrink-0" />}
              </div>
            </CardHeader>
          </Card>
        ))}
      </div>

      {/* Selector de identidad */}
      {rolSeleccionado === "organizador" && (
        <div className="space-y-2">
          <p className="text-sm font-medium text-foreground">¿Qué organizador sos?</p>
          {loadingOrgs ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" /> Cargando organizadores…
            </div>
          ) : (
            <Select value={orgId} onValueChange={setOrgId}>
              <SelectTrigger>
                <SelectValue placeholder="Seleccioná un organizador…" />
              </SelectTrigger>
              <SelectContent>
                {organizadores?.map((o) => (
                  <SelectItem key={o.organizadorId} value={o.organizadorId}>
                    {o.nombre}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        </div>
      )}

      {rolSeleccionado === "capitan" && (
        <div className="space-y-2">
          <p className="text-sm font-medium text-foreground">¿Cuál es tu equipo?</p>
          {loadingEquipos ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" /> Cargando equipos…
            </div>
          ) : (
            <Select value={equipoId} onValueChange={setEquipoId}>
              <SelectTrigger>
                <SelectValue placeholder="Seleccioná tu equipo…" />
              </SelectTrigger>
              <SelectContent>
                {equipos?.map((e) => (
                  <SelectItem key={e.equipoId} value={e.equipoId}>
                    [{e.tag}] {e.nombre}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        </div>
      )}

      {rolSeleccionado && (
        <Button className="w-full" onClick={ingresar}>
          Ingresar
        </Button>
      )}
    </div>
  );
}
