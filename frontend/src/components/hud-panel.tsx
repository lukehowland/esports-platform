import { cn } from "@/lib/utils";

interface HudPanelProps {
  children: React.ReactNode;
  className?: string;
  /** Tamaño del corte diagonal: 'sm' (8px) o 'md' (12px) */
  clip?: "sm" | "md";
  /** Borde violeta activo */
  active?: boolean;
  /** Borde lima de énfasis */
  accent?: "lime" | "violet";
}

/**
 * Panel con corte diagonal — elemento firma del diseño "Broadcast HUD".
 * Todos los paneles principales del sitio usan este contenedor.
 */
export function HudPanel({ children, className, clip = "md", active, accent }: HudPanelProps) {
  return (
    <div
      className={cn(
        "bg-panel border border-line",
        clip === "md" ? "hud-clip" : "hud-clip-sm",
        active && "border-violet/50 glow-violet-sm",
        accent === "lime" && "border-lime/40 glow-lime",
        accent === "violet" && "border-violet/50 glow-violet-sm",
        className
      )}
    >
      {children}
    </div>
  );
}

/** Eyebrow label — etiqueta mono uppercase para secciones HUD */
export function HudEyebrow({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <span className={cn("eyebrow", className)}>
      {children}
    </span>
  );
}
