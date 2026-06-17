import { cn } from "@/lib/utils";

interface RankingPositionProps {
  position: number;
  className?: string;
}

export function RankingPosition({ position, className }: RankingPositionProps) {
  return (
    <span
      className={cn(
        "inline-flex h-7 w-7 items-center justify-center hud-clip-sm text-xs font-mono font-bold",
        position === 1 && "bg-gold/15 text-gold border border-gold/40",
        position === 2 && "bg-silver/15 text-silver border border-silver/40",
        position === 3 && "bg-bronze/15 text-bronze border border-bronze/40",
        position > 3  && "bg-elevated text-muted-foreground border border-line",
        className
      )}
    >
      {position}
    </span>
  );
}
