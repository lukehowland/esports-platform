import { cn } from "@/lib/utils";

interface StatTileProps {
  value: string | number;
  label: string;
  className?: string;
  /** Color del valor: violet (default), lime, gold, muted */
  color?: "violet" | "lime" | "gold" | "muted" | "success" | "warning";
}

const colorMap: Record<NonNullable<StatTileProps["color"]>, string> = {
  violet:  "text-violet-bright",
  lime:    "text-lime",
  gold:    "text-gold",
  muted:   "text-muted-foreground",
  success: "text-success",
  warning: "text-warning",
};

/** Tile de estadística HUD: número grande + label mono */
export function StatTile({ value, label, className, color = "violet" }: StatTileProps) {
  return (
    <div className={cn("bg-elevated border border-line hud-clip-sm p-4 space-y-1", className)}>
      <div className={cn("text-3xl font-display font-bold leading-none tabular-nums", colorMap[color])}>
        {value}
      </div>
      <div className="eyebrow">{label}</div>
    </div>
  );
}
