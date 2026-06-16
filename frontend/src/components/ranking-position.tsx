import { cn } from "@/lib/utils";

interface RankingPositionProps {
  position: number;
  className?: string;
}

export function RankingPosition({ position, className }: RankingPositionProps) {
  return (
    <span
      className={cn(
        "inline-flex h-7 w-7 items-center justify-center rounded-full text-sm font-bold",
        position === 1 && "bg-gold/20 text-gold border border-gold/40",
        position === 2 && "bg-silver/20 text-silver border border-silver/40",
        position === 3 && "bg-bronze/20 text-bronze border border-bronze/40",
        position > 3 && "bg-secondary text-muted-foreground border border-border",
        className
      )}
    >
      {position}
    </span>
  );
}
