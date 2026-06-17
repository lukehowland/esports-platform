import { AlertTriangle } from "lucide-react";
import { Button } from "./ui/button";
import { ApiError } from "@/lib/api/fetcher";

interface ErrorStateProps {
  error: unknown;
  onRetry?: () => void;
}

export function ErrorState({ error, onRetry }: ErrorStateProps) {
  const message =
    error instanceof ApiError
      ? error.detail
      : error instanceof Error
      ? error.message
      : "Error desconocido";

  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <div className="hud-clip-sm border border-derrota/30 bg-derrota/5 p-4 mb-4">
        <AlertTriangle className="h-8 w-8 text-destructive" />
      </div>
      <p className="font-display font-semibold text-foreground tracking-wide">Error al cargar datos</p>
      <p className="text-sm text-muted-foreground mt-1 max-w-md">{message}</p>
      {onRetry && (
        <Button variant="outline" size="sm" className="mt-4" onClick={onRetry}>
          Reintentar
        </Button>
      )}
    </div>
  );
}
