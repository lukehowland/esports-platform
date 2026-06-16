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
      <AlertTriangle className="h-12 w-12 text-destructive mb-4" />
      <p className="text-base font-medium text-foreground">Ocurrió un error</p>
      <p className="text-sm text-muted-foreground mt-1 max-w-md">{message}</p>
      {onRetry && (
        <Button variant="outline" size="sm" className="mt-4" onClick={onRetry}>
          Reintentar
        </Button>
      )}
    </div>
  );
}
