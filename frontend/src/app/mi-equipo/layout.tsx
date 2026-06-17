"use client";

import { RequireRole } from "@/lib/auth/require-role";
import { WorkspaceTopbar } from "@/components/layout/workspace-topbar";

export default function MiEquipoLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireRole roles={["capitan"]}>
      <WorkspaceTopbar section="Mi equipo" />
      <main className="container mx-auto max-w-7xl px-4 py-6 min-h-[calc(100vh-3.5rem)]">
        {children}
      </main>
    </RequireRole>
  );
}
