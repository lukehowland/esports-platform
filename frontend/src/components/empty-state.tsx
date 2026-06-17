import { Inbox } from "lucide-react";
import { cn } from "@/lib/utils";

interface EmptyStateProps {
  title?: string;
  description?: string;
  className?: string;
}

export function EmptyState({ title = "Sin resultados", description = "No se encontraron datos.", className }: EmptyStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center py-16 text-center", className)}>
      <div className="hud-clip-sm border border-line bg-elevated p-4 mb-4">
        <Inbox className="h-8 w-8 text-muted-foreground opacity-40" />
      </div>
      <p className="font-display font-semibold text-foreground tracking-wide">{title}</p>
      <p className="text-sm text-muted-foreground mt-1">{description}</p>
    </div>
  );
}
