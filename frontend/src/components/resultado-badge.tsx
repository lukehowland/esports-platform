import { Badge } from "./ui/badge";
import { cn } from "@/lib/utils";

interface ResultadoBadgeProps {
  resultado: string;
  className?: string;
}

export function ResultadoBadge({ resultado, className }: ResultadoBadgeProps) {
  const esVictoria = resultado === "VICTORIA";
  const esDerrota = resultado === "DERROTA";

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-md px-2.5 py-0.5 text-xs font-semibold border",
        esVictoria && "border-success/30 bg-success/15 text-success",
        esDerrota && "border-destructive/30 bg-destructive/15 text-destructive",
        !esVictoria && !esDerrota && "border-border bg-secondary text-secondary-foreground",
        className
      )}
    >
      {resultado}
    </span>
  );
}
