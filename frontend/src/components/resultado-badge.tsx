import { cn } from "@/lib/utils";

interface ResultadoBadgeProps {
  resultado: string;
  className?: string;
}

export function ResultadoBadge({ resultado, className }: ResultadoBadgeProps) {
  const esVictoria = resultado === "VICTORIA";
  const esDerrota  = resultado === "DERROTA";

  return (
    <span
      className={cn(
        "inline-flex items-center hud-clip-sm px-2.5 py-0.5 text-xs font-mono font-bold uppercase tracking-wide border",
        esVictoria && "border-lime/40 bg-lime/10 text-lime",
        esDerrota  && "border-derrota/40 bg-derrota/10 text-derrota",
        !esVictoria && !esDerrota && "border-line bg-elevated text-muted-foreground",
        className
      )}
    >
      {esVictoria ? "▰▰ victoria" : esDerrota ? "▱▱ derrota" : resultado}
    </span>
  );
}
